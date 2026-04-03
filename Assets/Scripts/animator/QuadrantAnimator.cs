using UnityEngine;

public class QuadrantAnimator : MonoBehaviour
{
    public Sprite[] allSprites; // Drag all sliced sprites here
    public float fps = 10f;
    public int quadrant; // 0=TL, 1=TR, 2=BL, 3=BR

    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float timer;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (allSprites == null || allSprites.Length < 4) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0;

            // 1. How many frames are in the strip? 
            // (Total sprites divided by 2 rows of 2 columns)
            int totalFrames = allSprites.Length / 4;
            currentFrame = (currentFrame + 1) % totalFrames;

            // 2. Figure out the Column and Row based on the Quadrant ID
            // quadrant 0 = TL (col 0, row 0)
            // quadrant 1 = TR (col 1, row 0)
            // quadrant 2 = BL (col 0, row 1)
            // quadrant 3 = BR (col 1, row 1)
            int col = (quadrant == 1 || quadrant == 3) ? 1 : 0;
            int row = (quadrant == 2 || quadrant == 3) ? 1 : 0;

            // 3. THE FIXED MATH:
            // Index = (RowOffset) + (FrameOffset) + Column
            int rowOffset = row * (totalFrames * 2);
            int frameOffset = currentFrame * 2;
            int spriteIndex = rowOffset + frameOffset + col;

            if (spriteIndex < allSprites.Length)
                sr.sprite = allSprites[spriteIndex];
        }
    }

    public void Setup(Sprite[] sprites, int quadID)
    {
        allSprites = sprites;
        quadrant = quadID;
        currentFrame = 0;
    }
}