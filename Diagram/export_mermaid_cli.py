"""
Script Export Mermaid Diagrams sang PNG/SVG
Sử dụng Mermaid CLI (@mermaid-js/mermaid-cli)
"""

import os
import subprocess
from pathlib import Path
from typing import List, Tuple

# Configuration
MERMAID_DIR = Path(__file__).parent / 'mermaid_diagrams'
OUTPUT_PNG_DIR = Path(__file__).parent / 'diagrams_png'
OUTPUT_SVG_DIR = Path(__file__).parent / 'diagrams_svg'

# Colors for console output
class Color:
    GREEN = '\033[92m'
    RED = '\033[91m'
    YELLOW = '\033[93m'
    BLUE = '\033[94m'
    RESET = '\033[0m'
    BOLD = '\033[1m'

def ensure_directories():
    """Tạo thư mục output nếu chưa tồn tại"""
    OUTPUT_PNG_DIR.mkdir(exist_ok=True)
    OUTPUT_SVG_DIR.mkdir(exist_ok=True)
    print(f"{Color.GREEN}✅ Đã tạo thư mục output:{Color.RESET}")
    print(f"   📁 PNG: {OUTPUT_PNG_DIR}")
    print(f"   📁 SVG: {OUTPUT_SVG_DIR}\n")

def get_mermaid_files() -> List[Path]:
    """Lấy danh sách tất cả file .mmd"""
    files = list(MERMAID_DIR.glob('*.mmd'))
    return sorted(files)

def export_to_png(mmd_file: Path) -> Tuple[bool, str]:
    """Export file .mmd sang PNG"""
    output_file = OUTPUT_PNG_DIR / f"{mmd_file.stem}.png"
    
    try:
        # Chạy mmdc command với shell=True để tìm được command trong PATH
        cmd = f'npx mmdc -i "{mmd_file}" -o "{output_file}" -b transparent'
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30,
            shell=True  # Quan trọng: cho phép tìm mmdc trong PATH trên Windows
        )
        
        if result.returncode == 0:
            return True, str(output_file)
        else:
            return False, result.stderr
            
    except subprocess.TimeoutExpired:
        return False, "Timeout (>30s)"
    except FileNotFoundError:
        return False, "mmdc command not found. Please install: npm install -g @mermaid-js/mermaid-cli"
    except Exception as e:
        return False, str(e)

def export_to_svg(mmd_file: Path) -> Tuple[bool, str]:
    """Export file .mmd sang SVG"""
    output_file = OUTPUT_SVG_DIR / f"{mmd_file.stem}.svg"
    
    try:
        # Chạy mmdc command với shell=True để tìm được command trong PATH
        cmd = f'npx mmdc -i "{mmd_file}" -o "{output_file}"'
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30,
            shell=True  # Quan trọng: cho phép tìm mmdc trong PATH trên Windows
        )
        
        if result.returncode == 0:
            return True, str(output_file)
        else:
            return False, result.stderr
            
    except subprocess.TimeoutExpired:
        return False, "Timeout (>30s)"
    except FileNotFoundError:
        return False, "mmdc command not found"
    except Exception as e:
        return False, str(e)

def main():
    print(f"\n{Color.BOLD}{Color.BLUE}{'='*60}")
    print(f"  Mermaid Diagram Export Tool")
    print(f"  Sử dụng Mermaid CLI (mmdc)")
    print(f"{'='*60}{Color.RESET}\n")
    
    # Tạo thư mục output
    ensure_directories()
    
    # Lấy danh sách file
    mermaid_files = get_mermaid_files()
    
    if not mermaid_files:
        print(f"{Color.RED}❌ Không tìm thấy file .mmd nào trong {MERMAID_DIR}{Color.RESET}")
        return
    
    print(f"{Color.BLUE}📊 Tìm thấy {len(mermaid_files)} file Mermaid{Color.RESET}\n")
    
    # Export từng file
    success_count: int = 0
    fail_count: int = 0
    
    for i, mmd_file in enumerate(mermaid_files, 1):
        print(f"{Color.YELLOW}[{i}/{len(mermaid_files)}]{Color.RESET} {mmd_file.name}")
        
        # Export PNG
        success_png, result_png = export_to_png(mmd_file)
        if success_png:
            print(f"  {Color.GREEN}✅ PNG:{Color.RESET} {Path(result_png).name}")
            success_count += 1  # pyre-ignore[58]
        else:
            print(f"  {Color.RED}❌ PNG Failed:{Color.RESET} {result_png}")
            fail_count += 1  # pyre-ignore[58]
        
        # Export SVG
        success_svg, result_svg = export_to_svg(mmd_file)
        if success_svg:
            print(f"  {Color.GREEN}✅ SVG:{Color.RESET} {Path(result_svg).name}")
        else:
            print(f"  {Color.RED}❌ SVG Failed:{Color.RESET} {result_svg}")
        
        print()
    
    # Summary
    print(f"{Color.BOLD}{Color.BLUE}{'='*60}{Color.RESET}")
    print(f"{Color.GREEN}✅ Thành công: {success_count} file{Color.RESET}")
    if fail_count > 0:
        print(f"{Color.RED}❌ Thất bại: {fail_count} file{Color.RESET}")
    print(f"{Color.BOLD}{Color.BLUE}{'='*60}{Color.RESET}\n")
    
    print(f"{Color.GREEN}📁 Kết quả export:{Color.RESET}")
    print(f"   PNG: {OUTPUT_PNG_DIR}")
    print(f"   SVG: {OUTPUT_SVG_DIR}\n")

if __name__ == '__main__':
    main()
