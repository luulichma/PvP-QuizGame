import subprocess
import sys

# Install PyPDF2 if not available
try:
    from PyPDF2 import PdfReader
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "PyPDF2", "-q"])
    from PyPDF2 import PdfReader

pdf_path = r"f:\Nam 3 Ky 2\TTCS\PvP-QuizGame\Kế hoạch phát triển ứng dụng Game Quiz Android (1).pdf"

reader = PdfReader(pdf_path)
print(f"=== PDF has {len(reader.pages)} pages ===\n")

for i, page in enumerate(reader.pages):
    text = page.extract_text()
    print(f"--- Page {i+1} ---")
    print(text)
    print()
