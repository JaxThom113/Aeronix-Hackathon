#!/usr/bin/env python3
"""
Startup script for the Flask API
"""

import sys
from pathlib import Path

# Add the backend directory to Python path
backend_dir = Path(__file__).parent
sys.path.insert(0, str(backend_dir))

from api import app

if __name__ == "__main__":
    print("Starting Aeronix Hackathon API Server...")
    print("=" * 50)
    print("Available endpoints:")
    print("  GET  /health - Health check")
    print("  POST /upload - Upload and process single file")
    print("  POST /process - Process multiple files with custom processor")
    print("  POST /gemini - Mock Gemini processing with DOCX output")
    print("  POST /gemini-text - Mock Gemini processing (text only)")
    print("  POST /upload-and-gemini - Upload file, process with Gemini, return DOCX")
    print("=" * 50)
    print("Server starting on http://localhost:5000")
    print("Press Ctrl+C to stop the server")
    print()

    try:
        app.run(debug=True, host="0.0.0.0", port=5000)
    except KeyboardInterrupt:
        print("\nServer stopped by user")
    except Exception as e:
        print(f"Error starting server: {e}")
