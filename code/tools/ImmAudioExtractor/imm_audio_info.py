#!/usr/bin/env python3
"""
IMM Audio Information Extractor (Python wrapper)

This script provides a Python interface to extract audio information from IMM files.
It calls the native ImmAudioExtractor executable and parses the output.

Usage:
    python imm_audio_info.py info myfile.imm
    python imm_audio_info.py export myfile.imm ./output wav
"""

import sys
import os
import subprocess
import json
import re
from pathlib import Path
from typing import List, Dict, Optional

class AudioInfo:
    """Represents audio layer information from an IMM file"""
    
    def __init__(self):
        self.layer_name: str = ""
        self.position: tuple = (0.0, 0.0, 0.0)
        self.direction: tuple = (0.0, 0.0, -1.0)
        self.up: tuple = (0.0, 1.0, 0.0)
        self.spatial_type: str = ""
        self.attenuation_type: str = ""
        self.attenuation_min: float = 0.0
        self.attenuation_max: float = 0.0
        self.gain: float = 1.0
        self.volume: float = 1.0
        self.looping: bool = False
        self.channels: int = 0
        self.sample_rate: int = 0
        self.bit_depth: int = 0
        self.duration: float = 0.0
    
    def to_dict(self) -> Dict:
        """Convert to dictionary for JSON serialization"""
        return {
            "layer_name": self.layer_name,
            "position": {"x": self.position[0], "y": self.position[1], "z": self.position[2]},
            "direction": {"x": self.direction[0], "y": self.direction[1], "z": self.direction[2]},
            "up": {"x": self.up[0], "y": self.up[1], "z": self.up[2]},
            "spatial_type": self.spatial_type,
            "attenuation": {
                "type": self.attenuation_type,
                "min": self.attenuation_min,
                "max": self.attenuation_max
            } if self.attenuation_type else None,
            "gain": self.gain,
            "volume": self.volume,
            "looping": self.looping,
            "audio_format": {
                "channels": self.channels,
                "sample_rate": self.sample_rate,
                "bit_depth": self.bit_depth,
                "duration_seconds": self.duration
            }
        }
    
    def __str__(self) -> str:
        """Human-readable string representation"""
        lines = [
            f"Layer: {self.layer_name}",
            f"  Position: ({self.position[0]:.3f}, {self.position[1]:.3f}, {self.position[2]:.3f})",
            f"  Spatial Type: {self.spatial_type}",
        ]
        
        if self.attenuation_type:
            lines.append(f"  Attenuation: {self.attenuation_type} (Min: {self.attenuation_min:.2f}, Max: {self.attenuation_max:.2f})")
        
        lines.extend([
            f"  Gain: {self.gain:.2f}, Volume: {self.volume:.2f}",
            f"  Looping: {self.looping}",
            f"  Format: {self.channels}ch, {self.sample_rate}Hz, {self.bit_depth}bit",
            f"  Duration: {self.duration:.2f}s"
        ])
        
        return "\n".join(lines)


def find_extractor_executable() -> Optional[Path]:
    """Find the ImmAudioExtractor executable"""
    
    # Check common build locations
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent.parent.parent
    
    possible_paths = [
        script_dir / "ImmAudioExtractor.exe",
        script_dir / "ImmAudioExtractor",
        repo_root / "build" / "code" / "tools" / "ImmAudioExtractor" / "ImmAudioExtractor.exe",
        repo_root / "build" / "code" / "tools" / "ImmAudioExtractor" / "ImmAudioExtractor",
        repo_root / "build" / "Release" / "ImmAudioExtractor.exe",
        repo_root / "build" / "Debug" / "ImmAudioExtractor.exe",
    ]
    
    for path in possible_paths:
        if path.exists():
            return path
    
    return None


def parse_audio_info_output(output: str) -> List[AudioInfo]:
    """Parse the output from ImmAudioExtractor info command"""
    
    audio_infos = []
    current_info = None
    
    for line in output.split('\n'):
        line = line.strip()
        
        if line.startswith("=== Audio Layer:"):
            if current_info:
                audio_infos.append(current_info)
            current_info = AudioInfo()
            # Extract layer name
            match = re.search(r"=== Audio Layer: (.+) ===", line)
            if match:
                current_info.layer_name = match.group(1)
        
        elif current_info:
            if line.startswith("Position:"):
                match = re.search(r"Position: \(([^,]+), ([^,]+), ([^)]+)\)", line)
                if match:
                    current_info.position = (float(match.group(1)), float(match.group(2)), float(match.group(3)))
            
            elif line.startswith("Direction:"):
                match = re.search(r"Direction: \(([^,]+), ([^,]+), ([^)]+)\)", line)
                if match:
                    current_info.direction = (float(match.group(1)), float(match.group(2)), float(match.group(3)))
            
            elif line.startswith("Up:"):
                match = re.search(r"Up: \(([^,]+), ([^,]+), ([^)]+)\)", line)
                if match:
                    current_info.up = (float(match.group(1)), float(match.group(2)), float(match.group(3)))
            
            elif line.startswith("Spatial Type:"):
                current_info.spatial_type = line.split(":", 1)[1].strip()
            
            elif line.startswith("Attenuation:"):
                match = re.search(r"Attenuation: ([^(]+) \(Min: ([^,]+), Max: ([^)]+)\)", line)
                if match:
                    current_info.attenuation_type = match.group(1).strip()
                    current_info.attenuation_min = float(match.group(2))
                    current_info.attenuation_max = float(match.group(3))
            
            elif line.startswith("Gain:"):
                match = re.search(r"Gain: ([\d.]+)", line)
                if match:
                    current_info.gain = float(match.group(1))
            
            elif line.startswith("Volume:"):
                match = re.search(r"Volume: ([\d.]+)", line)
                if match:
                    current_info.volume = float(match.group(1))
            
            elif line.startswith("Looping:"):
                current_info.looping = "Yes" in line
            
            elif line.startswith("Channels:"):
                match = re.search(r"Channels: (\d+)", line)
                if match:
                    current_info.channels = int(match.group(1))
            
            elif line.startswith("Sample Rate:"):
                match = re.search(r"Sample Rate: (\d+)", line)
                if match:
                    current_info.sample_rate = int(match.group(1))

            elif line.startswith("Bit Depth:"):
                match = re.search(r"Bit Depth: (\d+)", line)
                if match:
                    current_info.bit_depth = int(match.group(1))

            elif line.startswith("Duration:"):
                match = re.search(r"Duration: ([\d.]+)", line)
                if match:
                    current_info.duration = float(match.group(1))

    if current_info:
        audio_infos.append(current_info)

    return audio_infos


def extract_audio_info(imm_file: str, output_format: str = "text") -> Optional[List[AudioInfo]]:
    """
    Extract audio information from an IMM file

    Args:
        imm_file: Path to the IMM file
        output_format: Output format - "text", "json", or "dict"

    Returns:
        List of AudioInfo objects, or None on error
    """

    extractor = find_extractor_executable()
    if not extractor:
        print("Error: Could not find ImmAudioExtractor executable", file=sys.stderr)
        print("Please build the tool first using CMake", file=sys.stderr)
        return None

    if not os.path.exists(imm_file):
        print(f"Error: IMM file not found: {imm_file}", file=sys.stderr)
        return None

    try:
        result = subprocess.run(
            [str(extractor), "info", imm_file],
            capture_output=True,
            text=True,
            check=True
        )

        audio_infos = parse_audio_info_output(result.stdout)

        if output_format == "json":
            print(json.dumps([info.to_dict() for info in audio_infos], indent=2))
        elif output_format == "text":
            print(f"\nFound {len(audio_infos)} audio layer(s)\n")
            for info in audio_infos:
                print(info)
                print()

        return audio_infos

    except subprocess.CalledProcessError as e:
        print(f"Error running ImmAudioExtractor: {e}", file=sys.stderr)
        print(f"Output: {e.stdout}", file=sys.stderr)
        print(f"Error: {e.stderr}", file=sys.stderr)
        return None


def export_audio(imm_file: str, output_folder: str, format: str = "wav") -> bool:
    """
    Export audio from an IMM file

    Args:
        imm_file: Path to the IMM file
        output_folder: Folder to export audio files to
        format: Audio format - "wav", "ogg", or "opus"

    Returns:
        True on success, False on error
    """

    extractor = find_extractor_executable()
    if not extractor:
        print("Error: Could not find ImmAudioExtractor executable", file=sys.stderr)
        print("Please build the tool first using CMake", file=sys.stderr)
        return False

    if not os.path.exists(imm_file):
        print(f"Error: IMM file not found: {imm_file}", file=sys.stderr)
        return False

    # Create output folder if it doesn't exist
    os.makedirs(output_folder, exist_ok=True)

    try:
        result = subprocess.run(
            [str(extractor), "export", imm_file, output_folder, format],
            capture_output=True,
            text=True,
            check=True
        )

        print(result.stdout)
        return True

    except subprocess.CalledProcessError as e:
        print(f"Error running ImmAudioExtractor: {e}", file=sys.stderr)
        print(f"Output: {e.stdout}", file=sys.stderr)
        print(f"Error: {e.stderr}", file=sys.stderr)
        return False


def main():
    """Main entry point"""

    if len(sys.argv) < 3:
        print("IMM Audio Information Extractor (Python wrapper)")
        print("\nUsage:")
        print("  python imm_audio_info.py info <imm_file> [--json]")
        print("  python imm_audio_info.py export <imm_file> <output_folder> [format]")
        print("\nExamples:")
        print("  python imm_audio_info.py info myfile.imm")
        print("  python imm_audio_info.py info myfile.imm --json")
        print("  python imm_audio_info.py export myfile.imm ./audio_output wav")
        print("  python imm_audio_info.py export myfile.imm ./audio_output opus")
        sys.exit(1)

    command = sys.argv[1]

    if command == "info":
        imm_file = sys.argv[2]
        output_format = "json" if "--json" in sys.argv else "text"

        result = extract_audio_info(imm_file, output_format)
        sys.exit(0 if result else 1)

    elif command == "export":
        if len(sys.argv) < 4:
            print("Error: export command requires output folder", file=sys.stderr)
            sys.exit(1)

        imm_file = sys.argv[2]
        output_folder = sys.argv[3]
        format = sys.argv[4] if len(sys.argv) > 4 else "wav"

        if format not in ["wav", "ogg", "opus"]:
            print(f"Error: Unsupported format '{format}'. Use wav, ogg, or opus", file=sys.stderr)
            sys.exit(1)

        result = export_audio(imm_file, output_folder, format)
        sys.exit(0 if result else 1)

    else:
        print(f"Error: Unknown command '{command}'", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

