using UnityEngine;
using UnityEngine.InputSystem;

public class RotationAnimatorInput : MonoBehaviour
{
    [SerializeField] private RotationAnimator rotationAnimator;

    private void Awake()
    {
        if (rotationAnimator == null)
        {
            rotationAnimator = GetComponent<RotationAnimator>();
        }
    }

    private void Update()
    {
        if (rotationAnimator == null || rotationAnimator.isAnimating) { return; }

        if (Keyboard.current == null) { return; }

        if (Keyboard.current.uKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.U);
        if (Keyboard.current.jKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.UPrime);

        if (Keyboard.current.dKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.D);
        if (Keyboard.current.cKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.DPrime);

        if (Keyboard.current.lKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.L);
        if (Keyboard.current.kKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.LPrime);

        if (Keyboard.current.rKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.R);
        if (Keyboard.current.eKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.RPrime);

        if (Keyboard.current.fKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.F);
        if (Keyboard.current.gKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.FPrime);

        if (Keyboard.current.bKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.B);
        if (Keyboard.current.nKey.wasPressedThisFrame) rotationAnimator.AnimateAndApplyRotation(RotationAnimator.RotationType.BPrime);
    }
}
