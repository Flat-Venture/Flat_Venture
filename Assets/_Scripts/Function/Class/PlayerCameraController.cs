using UnityEngine;
using Unity.Cinemachine;
public class PlayerCameraController : MonoBehaviour
{
    [Header("Cam Rotate")]
    public CinemachineCamera cineCam;
    CinemachineOrbitalFollow orbit;
    public Transform target;
    public float sensitivity = 10f;
    public float minAngle = 10f;
    public float maxAngle = 70f;

    bool dragging = false;
    Vector2 lastMousePos;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 15f;

    private void Start()
    {
        if (cineCam == null) cineCam = GetComponent<CinemachineCamera>();

        if (target == null) Debug.LogError("카메라가 따라갈 대상(Target)이 없다");

        orbit = cineCam.GetComponent<CinemachineOrbitalFollow>();

        if (orbit == null) Debug.Log("OrbitalFollow가 카메라에 없다");

        orbit.Radius = maxZoom;

    }

    private void Update()
    {
        if (Player.Instance.isDead) return;
        RightClickRotation();
        Zoom();
    }

    /// <summary>
    /// 우클릭으로 캐릭터 중심 카메라 회전
    /// </summary>
    void RightClickRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragging = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            //좌우 회전
            orbit.HorizontalAxis.Value += delta.x * sensitivity * Time.deltaTime;
            //상하 회전
            orbit.VerticalAxis.Value -= delta.y * sensitivity * Time.deltaTime;
            //피치 제한
            orbit.VerticalAxis.Value = Mathf.Clamp(orbit.VerticalAxis.Value, minAngle, maxAngle);
        }
        //타겟을 따라가야 하니까 포지션 잡아주기
        transform.position = target.position;
    }

    /// <summary>
    /// 스크롤로 줌인, 줌아웃
    /// </summary>
    void Zoom()
    {
        //스크롤 들고오기
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        //스크롤 반응이 되면
        if (scroll != 0f)
        {
            // vertical건드릴때 처럼 마이너스 해줘야 우리가 원하는 상황이 연출됨.
            orbit.Radius -= scroll * zoomSpeed;
            //스크롤 최대, 최솟값 설정
            orbit.Radius = Mathf.Clamp(orbit.Radius, minZoom, maxZoom);
        }
    }
}
