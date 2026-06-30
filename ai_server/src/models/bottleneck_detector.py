"""
Train mô hình GNN (Graph Neural Network) phát hiện điểm nghẽn.
Dùng dữ liệu từ:
  - data/processed/bottleneck_clean.csv  → node features
  - data/processed/phu_thuoc_clean.csv   → edges (cạnh đồ thị)

Chạy: python -m src.models.bottleneck_detector
"""

import pandas as pd
import numpy as np
import joblib
import torch
import torch.nn.functional as F
from torch_geometric.data import Data
from torch_geometric.nn import GCNConv
from pathlib import Path
from sklearn.preprocessing import LabelEncoder, MinMaxScaler
from sklearn.metrics import classification_report, roc_auc_score
from sklearn.model_selection import train_test_split  

# ============================================================
# ĐƯỜNG DẪN
# ============================================================
PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"
MODEL_DIR     = Path(__file__).resolve().parent.parent.parent / "models"
MODEL_DIR.mkdir(parents=True, exist_ok=True)

# ============================================================
# CẤU HÌNH
# ============================================================
NODE_FEATURE_COLS = [
    "SoGioUocTinh",
    "TrangThaiHienTai_Encoded",
]
LABEL_COL   = "Nhan_GhiNhanDiemNghen"
HIDDEN_DIM  = 64
NUM_EPOCHS  = 200
LEARNING_RATE = 0.01


# ============================================================
# BƯỚC 1: CHUẨN BỊ DỮ LIỆU ĐỒ THỊ
# ============================================================

def load_graph_data(fit_artifacts: bool = True):
    """
    Tải và chuẩn bị dữ liệu đồ thị:
    - Node: mỗi task là 1 node
    - Edge: phụ thuộc giữa các task
    - Node features: thông tin task
    - Label: task có phải điểm nghẽn không
    """
    print("   Đọc dữ liệu node (bottleneck_clean.csv)...")
    node_df = pd.read_csv(PROCESSED_DIR / "bottleneck_clean.csv")
    print(f"   Nodes: {len(node_df)} task")

    # Encode TrangThaiHienTai
    encoder_path = MODEL_DIR / "gnn_status_encoder.joblib"
    status_values = node_df["TrangThaiHienTai"].fillna("Unknown").astype(str)
    if fit_artifacts:
        le = LabelEncoder()
        le.fit(pd.concat([status_values, pd.Series(["Unknown"])]))
        node_df["TrangThaiHienTai_Encoded"] = le.transform(status_values)
        joblib.dump(le, encoder_path)
    else:
        le = joblib.load(encoder_path)
        status_values = status_values.where(status_values.isin(le.classes_), "Unknown")
        node_df["TrangThaiHienTai_Encoded"] = le.transform(status_values)

    # Fill null
    node_df["SoGioUocTinh"] = node_df["SoGioUocTinh"].fillna(
        node_df["SoGioUocTinh"].median()
    )
    node_df["SoTaskBiAnhHuongPhiaSau"] = node_df["SoTaskBiAnhHuongPhiaSau"].fillna(0)

    # Tính nhãn từ SoTaskBiAnhHuongPhiaSau
    # Node có nhiều task phụ thuộc → điểm nghẽn
    node_df[LABEL_COL] = node_df["SoTaskBiAnhHuongPhiaSau"].apply(
        lambda x: 1 if x >= 1 else 0
    )
    print(f"   Nhãn: {dict(node_df[LABEL_COL].value_counts())}")

    # Normalize features
    scaler_path = MODEL_DIR / "gnn_scaler.joblib"
    if fit_artifacts:
        scaler = MinMaxScaler()
        features = scaler.fit_transform(node_df[NODE_FEATURE_COLS])
        joblib.dump(scaler, scaler_path)
    else:
        scaler = joblib.load(scaler_path)
        features = scaler.transform(node_df[NODE_FEATURE_COLS])

    # Tạo mapping MaCongViec → index
    node_ids = node_df["MaCongViec"].values
    id_to_idx = {int(nid): idx for idx, nid in enumerate(node_ids)}

    # Đọc edges
    print("\n   Đọc dữ liệu cạnh (phu_thuoc_clean.csv)...")
    try:
        edge_df = pd.read_csv(PROCESSED_DIR / "phu_thuoc_clean.csv")
        print(f"   Edges: {len(edge_df)} cạnh")

        # Chuyển MaCongViec → index
        src_list, dst_list = [], []
        for _, row in edge_df.iterrows():
            src = int(row["MaCongViecTruoc"])
            dst = int(row["MaCongViecSau"])
            if src in id_to_idx and dst in id_to_idx:
                src_list.append(id_to_idx[src])
                dst_list.append(id_to_idx[dst])

        edge_index = torch.tensor([src_list, dst_list], dtype=torch.long)
        print(f"   Edges hợp lệ: {len(src_list)} cạnh")

    except FileNotFoundError:
        print("   Không có file phu_thuoc_clean.csv → tạo đồ thị không có cạnh")
        edge_index = torch.zeros((2, 0), dtype=torch.long)

    # Tạo PyTorch Geometric Data object
    x = torch.tensor(features, dtype=torch.float)
    y = torch.tensor(node_df[LABEL_COL].values, dtype=torch.long)

    data = Data(x=x, edge_index=edge_index, y=y)

    print(f"\n   Đồ thị: {data.num_nodes} nodes | {data.num_edges} edges | {data.num_features} features")

    return data, node_df, scaler


# ============================================================
# BƯỚC 2: ĐỊNH NGHĨA MODEL GNN
# ============================================================

class GCN(torch.nn.Module):
    """
    Graph Convolutional Network (GCN) — Kipf & Welling 2017
    
    Kiến trúc 3 tầng:
    Input → GCNConv(64) → ReLU → Dropout
          → GCNConv(32) → ReLU → Dropout
          → GCNConv(2)  → Softmax → Output (0/1)
    
    Mỗi tầng GCNConv:
    - Tổng hợp thông tin từ node láng giềng
    - Cập nhật embedding của node hiện tại
    """
    def __init__(self, num_features, hidden_dim=64):
        super(GCN, self).__init__()
        self.conv1 = GCNConv(num_features, hidden_dim)
        self.conv2 = GCNConv(hidden_dim, hidden_dim // 2)
        self.conv3 = GCNConv(hidden_dim // 2, 2)  # 2 class: 0/1

    def forward(self, x, edge_index):
        # Tầng 1
        x = self.conv1(x, edge_index)
        x = F.relu(x)
        x = F.dropout(x, p=0.3, training=self.training)

        # Tầng 2
        x = self.conv2(x, edge_index)
        x = F.relu(x)
        x = F.dropout(x, p=0.3, training=self.training)

        # Tầng 3 — output
        x = self.conv3(x, edge_index)
        return F.log_softmax(x, dim=1)


# ============================================================
# BƯỚC 3: TRAIN MODEL
# ============================================================

def train():
    print("=" * 60)
    print("TRAIN MODEL — GNN Phát Hiện Điểm Nghẽn")
    print("=" * 60)

    # --------------------------------------------------------
    # 1. TẢI DỮ LIỆU
    # --------------------------------------------------------
    print("\n📌 [1/4] Chuẩn bị dữ liệu đồ thị...")
    data, node_df, scaler = load_graph_data()

    # Chia train/test mask (80/20)
    num_nodes    = data.num_nodes
    labels_np    = data.y.numpy()
    label_counts = pd.Series(labels_np).value_counts()

    if len(label_counts) < 2:
        raise ValueError(f"GNN cần ít nhất 2 class để train: {dict(label_counts)}")
    if label_counts.min() < 2:
        raise ValueError(
            f"GNN cần ít nhất 2 node mỗi class để stratified split: "
            f"{dict(label_counts)}"
        )

    n_test  = int(np.ceil(0.2 * num_nodes))
    n_train = num_nodes - n_test
    if n_test < len(label_counts) or n_train < len(label_counts):
        raise ValueError(
            f"Không đủ node để stratified split: train={n_train}, "
            f"test={n_test}, classes={len(label_counts)}"
        )

    indices = np.arange(num_nodes)
    train_idx, test_idx = train_test_split(
        indices,
        test_size=0.2,
        random_state=42,
        stratify=labels_np,
)

    train_mask = torch.zeros(num_nodes, dtype=torch.bool)
    test_mask  = torch.zeros(num_nodes, dtype=torch.bool)
    train_mask[train_idx] = True
    test_mask[test_idx]   = True
    data.train_mask = train_mask
    data.test_mask  = test_mask

    print(f"   Train: {train_mask.sum().item()} nodes")
    print(f"   Test:  {test_mask.sum().item()} nodes")

    # --------------------------------------------------------
    # 2. KHỞI TẠO MODEL
    # --------------------------------------------------------
    print("\n📌 [2/4] Cấu hình GNN...")
    model     = GCN(num_features=data.num_features, hidden_dim=HIDDEN_DIM)
    optimizer = torch.optim.Adam(model.parameters(), lr=LEARNING_RATE, weight_decay=5e-4)
    # Tính class weight để xử lý mất cân bằng nhãn
    train_labels = data.y[data.train_mask]
    class_counts = torch.bincount(train_labels)
    class_weights = (1.0 / class_counts.float())
    class_weights = class_weights / class_weights.sum() * len(class_counts)
    print(f"   Class weights: {class_weights.tolist()}")

    print(f"   Kiến trúc: GCNConv({data.num_features}) → GCNConv(64) → GCNConv(32) → GCNConv(2)")
    print(f"   Optimizer: Adam | lr={LEARNING_RATE} | weight_decay=5e-4")
    print(f"   Epochs: {NUM_EPOCHS}")
    print("   Dropout: 0.3")

    # --------------------------------------------------------
    # 3. TRAIN
    # --------------------------------------------------------
    print("\n📌 [3/4] Train GNN...")

    best_loss   = float('inf')
    best_model  = None
    train_losses = []

    for epoch in range(NUM_EPOCHS):
        model.train()
        optimizer.zero_grad()

        out  = model(data.x, data.edge_index)
        # loss = F.nll_loss(out[data.train_mask], data.y[data.train_mask])
        loss = F.nll_loss(out[data.train_mask], data.y[data.train_mask], weight=class_weights)
        loss.backward()
        optimizer.step()

        train_losses.append(loss.item())

        # Lưu model tốt nhất
        if loss.item() < best_loss:
            best_loss  = loss.item()
            best_model = {k: v.clone() for k, v in model.state_dict().items()}

        if (epoch + 1) % 20 == 0:
            print(f"   Epoch {epoch+1:3d}/{NUM_EPOCHS} | Loss: {loss.item():.4f}")

    # Load lại model tốt nhất
    model.load_state_dict(best_model)

    # --------------------------------------------------------
    # 4. ĐÁNH GIÁ
    # --------------------------------------------------------
    print("\n📌 [4/4] Đánh giá model...")
    model.eval()
    with torch.no_grad():
        out    = model(data.x, data.edge_index)
        proba  = torch.exp(out[:, 1]).numpy()
        pred   = out.argmax(dim=1).numpy()
        labels = data.y.numpy()

    y_test      = labels[test_mask.numpy()]
    y_pred      = pred[test_mask.numpy()]
    y_proba     = proba[test_mask.numpy()]

    print("\n=== KẾT QUẢ ĐÁNH GIÁ ===")
    print(classification_report(
        y_test, y_pred,
        target_names=["Bình thường", "Điểm nghẽn"]
    ))

    roc_auc = roc_auc_score(y_test, y_proba)
    print(f"ROC-AUC: {roc_auc:.3f}")

    if roc_auc >= 0.85:
        print("✅ Model TỐT — ROC-AUC ≥ 0.85")
    elif roc_auc >= 0.70:
        print("⚠️  Model TRUNG BÌNH — ROC-AUC 0.70-0.85")
    else:
        print("❌ Model YẾU — ROC-AUC < 0.70")

    # --------------------------------------------------------
    # 5. LƯU MODEL
    # --------------------------------------------------------
    model_path    = MODEL_DIR / "gnn_model.pt"
    metadata_path = MODEL_DIR / "gnn_metadata.joblib"

    torch.save(model.state_dict(), model_path)
    print(f"\n✅ Model lưu tại: {model_path}")

    metadata = {
        "num_features":  data.num_features,
        "hidden_dim":    HIDDEN_DIM,
        "roc_auc":       round(roc_auc, 4),
        "num_nodes":     data.num_nodes,
        "num_edges":     data.num_edges,
        "node_feature_cols": NODE_FEATURE_COLS,
    }
    joblib.dump(metadata, metadata_path)
    print(f"✅ Metadata lưu tại: {metadata_path}")

    return model, data


# ============================================================
# PREDICT — dùng khi API gọi
# ============================================================

def predict_bottleneck(top_n: int = 10) -> list:
    """
    Phát hiện top N điểm nghẽn trong đồ thị hiện tại.
    
    Output: danh sách task có nguy cơ là điểm nghẽn cao nhất
    """
    if top_n < 1:
        raise ValueError("top_n phải >= 1")
    # Load model
    metadata = joblib.load(MODEL_DIR / "gnn_metadata.joblib")
    model    = GCN(
        num_features=metadata["num_features"],
        hidden_dim=metadata["hidden_dim"]
    )
    model.load_state_dict(torch.load(MODEL_DIR / "gnn_model.pt"))
    model.eval()

    # Load data
    data, node_df, _ = load_graph_data(fit_artifacts=False)

    with torch.no_grad():
        out   = model(data.x, data.edge_index)
        proba = torch.exp(out[:, 1]).numpy()

    # Xếp hạng theo xác suất bottleneck
    node_df["bottleneck_score"] = proba
    top_bottlenecks = (
        node_df[["MaCongViec", "SoTaskBiAnhHuongPhiaSau", "bottleneck_score"]]
        .sort_values("bottleneck_score", ascending=False)
        .head(top_n)
    )

    return top_bottlenecks.to_dict("records")


if __name__ == "__main__":
    train()