"""
Simple CLI test to exercise extract_file_content and merge_docx_files
"""

from cli import extract_file_content
from api import merge_docx_files
import os

TEST_FILES = [
    os.path.join(
        os.path.dirname(__file__), "test_files", "Clemson_HW_Spec_V4_092325.docx"
    ),
    os.path.join(os.path.dirname(__file__), "test_files", "Clemson_SW_Spec 2.1.docx"),
]

if __name__ == "__main__":
    print("Running CLI extraction on test files...")
    for f in TEST_FILES:
        r = extract_file_content(f)
        print(f"{os.path.basename(f)} -> type={r.get('type')} size={r.get('size')}")
        print("Preview:")
        print(r.get("content")[:500])
        print("-" * 40)

    print("Running merge...")
    out = merge_docx_files(TEST_FILES, "cli_merged.docx")
    print("Merged output saved to:", out)
