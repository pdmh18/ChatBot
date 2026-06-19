"""Route chat (placeholder - tích hợp LLM sau)."""
from fastapi import APIRouter
from src.api.schemas import ChatRequest, ChatResponse

router = APIRouter(prefix="/api", tags=["Chat"])


@router.post("/chat", response_model=ChatResponse)
def chat(req: ChatRequest):
    """
    Placeholder trả lời chat. Sau này thay bằng gọi LLM thực tế
    (OpenAI / Claude API / model tự host).
    """
    reply = f"[AI placeholder] Đã nhận: '{req.message}'. Tích hợp LLM thật ở đây."
    return ChatResponse(reply=reply)
