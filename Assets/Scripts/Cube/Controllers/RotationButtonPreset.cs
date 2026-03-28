using UnityEngine;

public class RotationButtonPreset : MonoBehaviour
{
    public enum FaceSquare
    {
        UpperLeft,
        UpperRight,
        LowerRight,
        LowerLeft
    }

    public enum ButtonSlot
    {
        Primary,
        Secondary
    }

    [SerializeField] private FaceSquare square = FaceSquare.UpperLeft;
    [SerializeField] private ButtonSlot slot = ButtonSlot.Primary;
    [SerializeField] private RotationButton rotationButton;

    private void Awake()
    {
        if (rotationButton == null)
        {
            rotationButton = GetComponent<RotationButton>();
        }

        ApplyPreset();
    }

    private void OnValidate()
    {
        if (rotationButton == null)
        {
            rotationButton = GetComponent<RotationButton>();
        }

        ApplyPreset();
    }

    private void ApplyPreset()
    {
        if (rotationButton == null) { return; }

        rotationButton.RotationType = GetRotationFor(square, slot);
    }

    private static RotationAnimator.RotationType GetRotationFor(FaceSquare square, ButtonSlot slot)
    {
        // Mapping based on user-defined scheme:
        // UL: Right column up, Bottom line left
        // UR: Left column up, Bottom line right
        // LR: Left column down, Upper line right
        // LL: Right column down, Upper line left
        switch (square)
        {
            case FaceSquare.UpperLeft:
                return slot == ButtonSlot.Primary
                    ? RotationAnimator.RotationType.R
                    : RotationAnimator.RotationType.DPrime; // Bottom line left

            case FaceSquare.UpperRight:
                return slot == ButtonSlot.Primary
                    ? RotationAnimator.RotationType.L
                    : RotationAnimator.RotationType.D; // Bottom line right

            case FaceSquare.LowerRight:
                return slot == ButtonSlot.Primary
                    ? RotationAnimator.RotationType.LPrime
                    : RotationAnimator.RotationType.U; // Upper line right

            case FaceSquare.LowerLeft:
                return slot == ButtonSlot.Primary
                    ? RotationAnimator.RotationType.RPrime
                    : RotationAnimator.RotationType.UPrime; // Upper line left

            default:
                return RotationAnimator.RotationType.U;
        }
    }
}
