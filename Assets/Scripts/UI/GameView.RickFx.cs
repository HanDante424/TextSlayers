using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 릭 엔더빌 권총 (사용자 2026-11-28: 원거리는 권총, 근거리는 검). 검 휘두르기는 GameView.SheetFx.cs 의 RickCleave.
        // rick_pistol: Docs/Design/Rick_Enderville_Pistol_64.png 를 절반(32×32, 2×2 칸의 다수 색)으로 줄인 것. 총구가 오른쪽.
        const int RickPistolCanvas = 32;
        const float RickPistolGripX = 11f, RickPistolGripY = 19f;   // 권총 도트에서 손잡이 위치
        const float RickPistolSpinX = 15f, RickPistolSpinY = 16f;   // 장전할 때 돌리는 축 (방아쇠울)
        const float RickHandX = 41f, RickHandY = 46f;               // 릭 도트(c12)에서 손 위치, 오른쪽을 볼 때 [임시]
        const float RickSwordShowTime = 0.8f;                       // 검을 휘두른 뒤 이 시간 동안은 권총을 감춘다 [임시]

        /// <summary>손의 권총. 방금 검을 썼으면 그리지 않는다. 발사 반동 · 장전 회전은 벤데타와 같은 계산을 쓴다.</summary>
        static void DrawRickPistol(Rect body, bool faceLeft, FieldUnit u, float now)
        {
            if (Event.current.type != EventType.Repaint || now - u.LastBladeTime < RickSwordShowTime) return;
            var texture = WeaponTexture("rick_pistol");
            if (texture == null) return;

            float pixel = body.width / FieldSpriteCanvasSize;
            float size = RickPistolCanvas * pixel;
            float handX = faceLeft ? FieldSpriteCanvasSize - RickHandX : RickHandX;
            float gripX = faceLeft ? RickPistolCanvas - RickPistolGripX : RickPistolGripX;
            var rect = new Rect(body.x + (handX - gripX) * pixel, body.y + (RickHandY - RickPistolGripY) * pixel, size, size);

            float angle = PistolAngle(u, now, out bool reloading);
            var previous = GUI.matrix;
            if (angle != 0f)   // 반동은 손잡이를, 장전 회전은 방아쇠울을 축으로
            {
                float axisX = reloading ? RickPistolSpinX : RickPistolGripX, axisY = reloading ? RickPistolSpinY : RickPistolGripY;
                if (faceLeft) axisX = RickPistolCanvas - axisX;
                Vector3 pivot = new Vector2(rect.x + axisX * pixel, rect.y + axisY * pixel);
                GUI.matrix = previous * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, faceLeft ? -angle : angle), Vector3.one)
                                      * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            }
            PaintFieldSprite(rect, texture, faceLeft, Color.white);
            GUI.matrix = previous;
        }
    }
}
