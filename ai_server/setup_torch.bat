@echo off
echo === Cai torch va torch_geometric (CPU) ===
pip install torch==2.2.2 --index-url https://download.pytorch.org/whl/cpu
pip install torch_geometric
echo === Xong! ===
pause