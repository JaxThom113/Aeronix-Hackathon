#!/usr/bin/env python3
"""
CLI Tool for handling multiple file types
Supports: text, json, csv, images, pdf, and more
"""

import typer
import json
import csv
import os
from pathlib import Path
from typing import List, Optional
import mimetypes
from PIL import Image
import pandas as pd

app = typer.Typer(help="Multi-file type CLI tool for Aeronix Hackathon")


def detect_file_type(file_path: str) -> str:
    """Detect file type based on extension and MIME type"""
    path = Path(file_path)
    mime_type, _ = mimetypes.guess_type(file_path)

    if mime_type:
        if mime_type.startswith("text/"):
            return "text"
        elif mime_type == "application/json":
            return "json"
        elif mime_type == "text/csv":
            return "csv"
        elif mime_type.startswith("image/"):
            return "image"
        elif mime_type == "application/pdf":
            return "pdf"

    # Fallback to extension
    ext = path.suffix.lower()
    if ext in [".txt", ".md", ".py", ".js", ".html", ".css"]:
        return "text"
    elif ext == ".json":
        return "json"
    elif ext == ".csv":
        return "csv"
    elif ext in [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff"]:
        return "image"
    elif ext == ".pdf":
        return "pdf"
    elif ext in [".xlsx", ".xls"]:
        return "excel"

    return "unknown"


def process_text_file(file_path: str) -> dict:
    """Process text files"""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        return {
            "type": "text",
            "file": file_path,
            "size": len(content),
            "lines": len(content.splitlines()),
            "words": len(content.split()),
            "preview": content[:200] + "..." if len(content) > 200 else content,
        }
    except Exception as e:
        return {"type": "text", "file": file_path, "error": str(e)}


def process_json_file(file_path: str) -> dict:
    """Process JSON files"""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        return {
            "type": "json",
            "file": file_path,
            "size": os.path.getsize(file_path),
            "keys": (
                list(data.keys())
                if isinstance(data, dict)
                else f"Array with {len(data)} items"
            ),
            "preview": (
                json.dumps(data, indent=2)[:200] + "..."
                if len(str(data)) > 200
                else json.dumps(data, indent=2)
            ),
        }
    except Exception as e:
        return {"type": "json", "file": file_path, "error": str(e)}


def process_csv_file(file_path: str) -> dict:
    """Process CSV files"""
    try:
        df = pd.read_csv(file_path)
        return {
            "type": "csv",
            "file": file_path,
            "size": os.path.getsize(file_path),
            "rows": len(df),
            "columns": list(df.columns),
            "preview": df.head().to_string(),
        }
    except Exception as e:
        return {"type": "csv", "file": file_path, "error": str(e)}


def process_image_file(file_path: str) -> dict:
    """Process image files"""
    try:
        with Image.open(file_path) as img:
            return {
                "type": "image",
                "file": file_path,
                "size": os.path.getsize(file_path),
                "dimensions": img.size,
                "mode": img.mode,
                "format": img.format,
            }
    except Exception as e:
        return {"type": "image", "file": file_path, "error": str(e)}


def process_pdf_file(file_path: str) -> dict:
    """Process PDF files (basic info)"""
    try:
        return {
            "type": "pdf",
            "file": file_path,
            "size": os.path.getsize(file_path),
            "note": "PDF processing requires additional libraries like PyPDF2",
        }
    except Exception as e:
        return {"type": "pdf", "file": file_path, "error": str(e)}


def process_file(file_path: str) -> dict:
    """Process a single file based on its type"""
    file_type = detect_file_type(file_path)

    if not os.path.exists(file_path):
        return {"type": file_type, "file": file_path, "error": "File not found"}

    processors = {
        "text": process_text_file,
        "json": process_json_file,
        "csv": process_csv_file,
        "image": process_image_file,
        "pdf": process_pdf_file,
    }

    processor = processors.get(
        file_type,
        lambda x: {"type": file_type, "file": x, "error": "Unsupported file type"},
    )
    return processor(file_path)


@app.command()
def analyze(
    files: List[str] = typer.Argument(..., help="File paths to analyze"),
    output: Optional[str] = typer.Option(
        None, "--output", "-o", help="Output file for results"
    ),
    format: str = typer.Option(
        "table", "--format", "-f", help="Output format: table, json, csv"
    ),
):
    """Analyze multiple files of different types"""
    results = []

    for file_path in files:
        typer.echo(f"Processing: {file_path}")
        result = process_file(file_path)
        results.append(result)

    if format == "json":
        output_data = json.dumps(results, indent=2)
    elif format == "csv":
        # Convert to CSV format
        if results:
            df = pd.DataFrame(results)
            output_data = df.to_csv(index=False)
        else:
            output_data = ""
    else:  # table format
        output_data = "\n".join(
            [
                f"File: {r['file']} | Type: {r['type']} | Size: {r.get('size', 'N/A')} bytes"
                + (f" | Error: {r['error']}" if "error" in r else "")
                for r in results
            ]
        )

    if output:
        with open(output, "w") as f:
            f.write(output_data)
        typer.echo(f"Results saved to: {output}")
    else:
        typer.echo(output_data)


@app.command()
def convert(
    input_file: str = typer.Argument(..., help="Input file path"),
    output_file: str = typer.Argument(..., help="Output file path"),
    format: str = typer.Option(
        "json", "--format", "-f", help="Output format: json, csv, txt"
    ),
):
    """Convert files between different formats"""
    file_type = detect_file_type(input_file)

    if file_type == "csv" and format == "json":
        try:
            df = pd.read_csv(input_file)
            df.to_json(output_file, orient="records", indent=2)
            typer.echo(f"Converted {input_file} to {output_file}")
        except Exception as e:
            typer.echo(f"Error: {e}")
    elif file_type == "json" and format == "csv":
        try:
            with open(input_file, "r") as f:
                data = json.load(f)
            df = pd.DataFrame(data)
            df.to_csv(output_file, index=False)
            typer.echo(f"Converted {input_file} to {output_file}")
        except Exception as e:
            typer.echo(f"Error: {e}")
    else:
        typer.echo(f"Conversion from {file_type} to {format} not supported yet")


@app.command()
def batch(
    directory: str = typer.Argument(..., help="Directory to process"),
    pattern: str = typer.Option(
        "*", "--pattern", "-p", help="File pattern (e.g., *.txt, *.json)"
    ),
    recursive: bool = typer.Option(
        False, "--recursive", "-r", help="Process subdirectories"
    ),
):
    """Process all files in a directory matching a pattern"""
    from glob import glob

    if recursive:
        search_pattern = os.path.join(directory, "**", pattern)
    else:
        search_pattern = os.path.join(directory, pattern)

    files = glob(search_pattern, recursive=recursive)

    if not files:
        typer.echo(f"No files found matching pattern: {pattern}")
        return

    typer.echo(f"Found {len(files)} files to process")

    results = []
    for file_path in files:
        result = process_file(file_path)
        results.append(result)

    # Display summary
    type_counts = {}
    for result in results:
        file_type = result["type"]
        type_counts[file_type] = type_counts.get(file_type, 0) + 1

    typer.echo("\nFile type summary:")
    for file_type, count in type_counts.items():
        typer.echo(f"  {file_type}: {count} files")


@app.command()
def info(file: str = typer.Argument(..., help="File to get detailed info about")):
    """Get detailed information about a single file"""
    result = process_file(file)

    typer.echo(f"\nFile Information:")
    typer.echo(f"  Path: {result['file']}")
    typer.echo(f"  Type: {result['type']}")

    if "error" in result:
        typer.echo(f"  Error: {result['error']}")
    else:
        for key, value in result.items():
            if key not in ["file", "type"]:
                typer.echo(f"  {key.title()}: {value}")


if __name__ == "__main__":
    app()
