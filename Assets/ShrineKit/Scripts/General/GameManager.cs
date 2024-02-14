using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //references
    public static GameManager singleton;
    public CameraMan camMan { get; private set; }
    public MouseMode mouseMode { get; private set; }
    public enum MouseMode
	{
        UI,
        Game
	}
    private float defaultFixedDeltaTime;
    public static int currentLevel => SceneManager.GetActiveScene().buildIndex;

    private void Awake ()
	{
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        camMan = GameObject.Find("_CAMERAMAN").GetComponent<CameraMan>();
    }

	private void Start()
	{
        //temp
        SetMouseMode(MouseMode.Game);
	}

	public void SetMouseMode (MouseMode mode)
	{
        mouseMode = mode;
        Cursor.visible = (mode == MouseMode.UI);
        Cursor.lockState = (mode == MouseMode.Game) ? CursorLockMode.Locked : CursorLockMode.None;
	}

    public void PauseGame ()
	{
        Settings.paused = true;
        ModTimescale(0.0f);
    }

    public void UnpauseGame ()
	{
        Settings.paused = false;
        ModTimescale(1.0f);
        SetMouseMode(MouseMode.Game);
    }

    private void ModTimescale (float timeScale)
	{
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }
}
