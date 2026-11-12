using System.Collections.Generic;
using UnityEngine;

public class ClothingFollow : MonoBehaviour
{
    [System.Serializable]
    public class Pose
    {
        public Sprite bodySprite;
        public Vector2 offset;
        public float rotation;
    }

    public SpriteRenderer body;
    public List<Pose> poses = new List<Pose>();

    Vector3 basePos;
    Quaternion baseRot;

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
    }

    void LateUpdate()
    {
        if (body == null) return;
        Vector2 off = Vector2.zero;
        float rot = 0f;
        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].bodySprite == body.sprite)
            {
                off = poses[i].offset;
                rot = poses[i].rotation;
                break;
            }
        }
        transform.localPosition = basePos + (Vector3)off;
        transform.localRotation = baseRot * Quaternion.Euler(0, 0, rot);
    }

    [ContextMenu("Save Current Position For This Frame")]
    void SaveCurrent()
    {
        if (body == null || body.sprite == null) return;
        Pose p = poses.Find(x => x.bodySprite == body.sprite);
        if (p == null)
        {
            p = new Pose { bodySprite = body.sprite };
            poses.Add(p);
        }
        p.offset = (Vector2)(transform.localPosition - basePos);
        p.rotation = Mathf.DeltaAngle(baseRot.eulerAngles.z, transform.localEulerAngles.z);
    }
}