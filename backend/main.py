#!/usr/bin/env python3
"""
Main entry point for Aeronix Hackathon application
"""

import sys
from pathlib import Path

# Add the backend directory to Python path
backend_dir = Path(__file__).parent
sys.path.insert(0, str(backend_dir))

from cli import app

if __name__ == "__main__":
    app()
