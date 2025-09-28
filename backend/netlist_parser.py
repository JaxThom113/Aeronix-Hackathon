import re
from collections import defaultdict

import langchain
import getpass
import os

if not os.environ.get("GOOGLE_API_KEY"):
  os.environ["GOOGLE_API_KEY"] = "" # put Gemini API key here

from langchain.chat_models import init_chat_model

llm = init_chat_model("gemini-2.5-flash", model_provider="google_genai")

def parse_netlist(file_path):
    if file_path.endswith(".d356"):
        return parse_d356(file_path)
    elif file_path.endswith(".ipc"):
        return parse_ipc(file_path)

def parse_d356(file_path):
    metadata = {}
    components = defaultdict(lambda: {"pins": []})
    nets = defaultdict(lambda: {"connections": []})

    # Regex for property lines (P ...)
    prop_pattern = re.compile(r"^P\s+(\S+)\s+(.+)$")

    # Regex for net lines: <net><name>   <component>   <geom>
    netline_pattern = re.compile(r"^(?P<netid>\d+)(?P<netname>\S*)\s+(?P<comp>\S+)\s+(?P<geom>.+)$")

    # Regex to pull coords/rotation/etc out of geometry string
    geom_pattern = re.compile(
        r"X(?P<x>[+-]?\d+)Y(?P<y>[+-]?\d+).*?"
        r"R(?P<rot>\d+).*?"
        r"S(?P<sid>\d+)"
    )

    with open(file_path, "r") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue

            # Handle metadata lines
            if line.startswith("P "):
                match = prop_pattern.match(line)
                if match:
                    key, value = match.groups()
                    metadata[key] = value.strip()
                continue

            # Handle net/component lines
            match = netline_pattern.match(line)
            if match:
                net_id = match.group("netid")
                net_name = match.group("netname")
                comp = match.group("comp")
                geom = match.group("geom")

                geom_match = geom_pattern.search(geom)
                if geom_match:
                    x = int(geom_match.group("x"))
                    y = int(geom_match.group("y"))
                    rot = int(geom_match.group("rot"))
                    sid = geom_match.group("sid")
                else:
                    x = y = rot = None
                    sid = None

                # Add component pin info
                pin_info = {
                    "net_id": net_id,
                    "net_name": net_name,
                    "component": comp,
                    "geom": geom,
                    "x": x,
                    "y": y,
                    "rotation": rot,
                    "symbol_id": sid,
                }
                components[comp]["pins"].append(pin_info)

                # Add net connection
                nets[net_id]["name"] = net_name
                nets[net_id]["connections"].append(f"{comp}")

    # Build final schema
    return {
        "metadata": metadata,
        "components": dict(components),
        "nets": dict(nets),
    }


def parse_ipc(file_path):
    nets = []

    # Regex to capture lines like:
    # 327N/C    U11   -15   PA01X 029311Y 015709X0709Y0315R270 S0
    pattern = re.compile(
        r'(?P<net>\S+)\s+'
        r'(?P<comp>[A-Za-z0-9]+)\s+'
        r'(?P<pin>-?\d+)\s+'
        r'PA\d+X\s*(?P<x>\d+)Y\s*(?P<y>\d+).*R(?P<rot>\d+)\s+S(?P<side>\d+)'
    )

    with open(file_path, 'r') as f:
        for line in f:
            match = pattern.search(line)
            if match:
                nets.append({
                    "net": match.group("net"),
                    "component": match.group("comp"),
                    "pin": match.group("pin"),
                    "x": int(match.group("x")),
                    "y": int(match.group("y")),
                    "rotation": int(match.group("rot")),
                    "side": match.group("side")
                })

    return nets


# def flatten_netlist(parsed):
#     snippets = []

#     print(parsed)

#     if "components" in parsed:  # d356 format
#         for comp, data in parsed["components"].items():
#             for pin in data["pins"]:
#                 snippet = f"Component {comp}, Net {pin['net_name']} ({pin['net_id']}), Pin at ({pin['x']},{pin['y']}), Rotation {pin['rotation']}"
#                 snippets.append(snippet)

#     elif "net" in parsed:  # ipc format
#         for net in parsed["net"]:
#             snippet = f"Net {net['net']}, Component {net['component']}, Pin {net['pin']}, Pos ({net['x']},{net['y']}), Rot {net['rotation']}, Side {net['side']}"
#             snippets.append(snippet)

#     return snippets

def flatten_netlist(parsed):
    snippets = []

    # Handle d356 format
    if "components" in parsed and "nets" in parsed:
        # Add metadata as a single chunk
        if parsed.get("metadata"):
            meta_chunk = "Metadata:\n" + "\n".join([f"{k}: {v}" for k, v in parsed["metadata"].items()])
            snippets.append(meta_chunk)

        # Chunk per component, including all pins and their nets
        for comp_name, comp_data in parsed.get("components", {}).items():
            pin_lines = []
            for pin in comp_data.get("pins", []):
                pin_lines.append(
                    f"Pin connected to net '{pin['net_name']}' (ID: {pin['net_id']}) at ({pin['x']}, {pin['y']}), rotation {pin['rotation']}, symbol {pin['symbol_id']}"
                )
            comp_chunk = f"Component: {comp_name}\n" + "\n".join(pin_lines)
            snippets.append(comp_chunk)

        # Chunk per net, listing all connected components
        for net_id, net_data in parsed.get("nets", {}).items():
            connections = net_data.get("connections", [])
            net_chunk = f"Net: {net_id} ({net_data.get('name', '')})\nConnected components: {', '.join(connections)}"
            snippets.append(net_chunk)

    # Handle ipc format (list of dicts)
    elif isinstance(parsed, list):
        # Group by net for better context
        net_groups = defaultdict(list)
        for entry in parsed:
            net_groups[entry["net"]].append(entry)

        for net, entries in net_groups.items():
            lines = []
            for e in entries:
                lines.append(
                    f"Component {e['component']}, Pin {e['pin']}, Pos ({e['x']},{e['y']}), Rot {e['rotation']}, Side {e['side']}"
                )
            net_chunk = f"Net: {net}\n" + "\n".join(lines)
            snippets.append(net_chunk)

    return snippets


from langchain_google_genai import GoogleGenerativeAIEmbeddings, ChatGoogleGenerativeAI
from langchain_community.vectorstores import Chroma
from langchain.chains import RetrievalQA
from langchain_community.embeddings import HuggingFaceEmbeddings
from docx import Document
from collections import defaultdict

def build_pipeline(netlist_file, extra_docs=["backend/test_files/Clemson_HW_Spec_V4_092325.docx", "backend/test_files/Clemson_SW_Spec 2.1.docx", "backend/AE304196-001_LoRa Car Radio Bring-Up Procedure.docx"]):
    # Step 1: Parse
    parsed = parse_netlist(netlist_file)

    # Step 2: Flatten to text
    snippets = flatten_netlist(parsed)
    print(snippets[:5])  # peek at first 5
    print("Number of snippets:", len(snippets))

    # Step 2b: Add extra docs (Word files)
    if extra_docs:
        for doc_path in extra_docs:
            doc = Document(doc_path)
            doc_text = "\n".join([para.text for para in doc.paragraphs if para.text.strip()])
            #print(doc_text)
            snippets.append(doc_text)
    
    print("Number of snippets:", len(snippets))

    # Step 3: Embeddings + Vectorstore
    #embedder = GoogleGenerativeAIEmbeddings(model="models/embedding-001")
    embedder = HuggingFaceEmbeddings(model_name="all-MiniLM-L6-v2")
    vectorstore = Chroma.from_texts(texts=snippets, embedding=embedder, persist_directory="./chroma_db")
    vectorstore.persist()  # optional: saves to disk

    # Step 4: LLM
    llm = ChatGoogleGenerativeAI(model="gemini-2.5-flash")

    # Step 5: RetrievalQA chain
    retriever = vectorstore.as_retriever(search_type="similarity", search_kwargs={"k":143})
    qa_chain = RetrievalQA.from_chain_type(llm=llm, chain_type="stuff", retriever=retriever)

    return qa_chain

if __name__ == "__main__":
    qa = build_pipeline("backend/test_files/Assembly Testpoint Report for Car-PCB1.ipc")
    question = '''You are a useful tool used by engineers to generate bring-up test plans. Use the provided files, which may include a netlist, BOM,  
                  hardware requirement document, and software requirement document. Additionally, reference the
                  provided example bring-up test document for guidance.'''
    answer = qa.run(question)
    print(answer)



#if __name__ == "__main__":
    #print(parse_netlist("backend/test_files/Assembly Testpoint Report for Car-PCB1.ipc"))
    #print(parse_netlist("backend/test_files/UNO-TH_Rev3e.d356"))