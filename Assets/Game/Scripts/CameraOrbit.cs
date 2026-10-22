using UnityEngine;

// กล้องหมุนช้า ๆ รอบจุดกลางฉาก (ใช้ในฉากทดสอบ)
public class CameraOrbit : MonoBehaviour
{
    public Vector3 target = new Vector3(0, 0, -1);
    public float speed = 6f;
    float radius, height, angle;

    void Start()
    {
        var d = transform.position - target;
        height = d.y;
        radius = new Vector2(d.x, d.z).magnitude;
        angle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;
        var a = angle * Mathf.Deg2Rad;
        transform.position = target + new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        transform.LookAt(target);
    }
}
