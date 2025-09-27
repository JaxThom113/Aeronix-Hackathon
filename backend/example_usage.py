#!/usr/bin/env python3
"""
Example usage of the CLI tool with content extraction and processing
"""

from cli import set_content_processor, extract_file_content
import typer


def my_custom_processor(
    content: str, file_type: str, file_path: str, processor_name: str = "default"
):
    """Custom processor function for handling file contents"""

    if processor_name == "analyze":
        # Analyze the content
        words = content.split()
        lines = content.split("\n")
        sentences = content.split(".")

        return {
            "word_count": len(words),
            "line_count": len(lines),
            "sentence_count": len(sentences),
            "avg_words_per_sentence": len(words) / len(sentences) if sentences else 0,
            "file_type": file_type,
        }

    elif processor_name == "extract_keywords":
        # Simple keyword extraction
        words = content.lower().split()
        # Remove common words
        stop_words = {
            "the",
            "a",
            "an",
            "and",
            "or",
            "but",
            "in",
            "on",
            "at",
            "to",
            "for",
            "of",
            "with",
            "by",
            "is",
            "are",
            "was",
            "were",
            "be",
            "been",
            "have",
            "has",
            "had",
            "do",
            "does",
            "did",
            "will",
            "would",
            "could",
            "should",
        }
        keywords = [word for word in words if word not in stop_words and len(word) > 3]

        # Count frequency
        from collections import Counter

        word_freq = Counter(keywords)
        top_keywords = word_freq.most_common(10)

        return {"top_keywords": top_keywords, "total_unique_words": len(set(keywords))}

    elif processor_name == "summarize":
        # Simple summarization (first and last sentences)
        sentences = [s.strip() for s in content.split(".") if s.strip()]
        if len(sentences) >= 2:
            summary = f"{sentences[0]}. {sentences[-1]}."
        else:
            summary = content[:200] + "..." if len(content) > 200 else content

        return {
            "summary": summary,
            "original_length": len(content),
            "summary_length": len(summary),
        }

    else:
        return f"Processed {file_type} file with {len(content)} characters"


def main():
    """Example of how to use the CLI tool programmatically"""

    # Set up the custom processor
    set_content_processor(my_custom_processor)

    # Example files to process
    test_files = [
        "test_files/sample.txt",
        "test_files/sample.json",
        "test_files/sample.csv",
    ]

    print("=== Content Extraction Example ===")

    for file_path in test_files:
        print(f"\nProcessing: {file_path}")

        # Extract content
        content_result = extract_file_content(file_path)

        if "error" in content_result:
            print(f"Error: {content_result['error']}")
            continue

        print(f"File type: {content_result['type']}")
        print(f"Content size: {content_result['size']} characters")

        # Process with different processors
        processors = ["analyze", "extract_keywords", "summarize"]

        for processor in processors:
            try:
                result = my_custom_processor(
                    content_result["content"],
                    content_result["type"],
                    content_result["file"],
                    processor,
                )
                print(f"  {processor}: {result}")
            except Exception as e:
                print(f"  {processor}: Error - {e}")


if __name__ == "__main__":
    main()
