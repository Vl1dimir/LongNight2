using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Scene;
using HFPS.Prefs;

public class SceneLoaderAdditive : MonoBehaviour
{
    public List<LoadStack> sceneInfos = new List<LoadStack>();

    [Space(7)]

    public FadePanelControl fadeController;
    public TipsManager tipsManager;
    public GameObject SpinnerGO;
    public Text sceneName;
    public Text sceneDescription;
    public Image backgroundImg;
    public GameObject manuallySwitchText;

    private GameObject MainCamera;

    private Dictionary<string, LoadStack> scenesStacks;

    [Tooltip("Switch scene by pressing any button")]
    public bool SwitchManually;

    [Space(7)]

    [Tooltip("Background Loading Priority")]
    public UnityEngine.ThreadPriority threadPriority = UnityEngine.ThreadPriority.High;
    public int timeBeforeLoad;

    void Start()
    {

        Time.timeScale = 1f;
        SpinnerGO.SetActive(true);
        manuallySwitchText.SetActive(false);
        if (tipsManager)
        {
            tipsManager.TipsText.gameObject.SetActive(true);
        }

        SceneTool.threadPriority = threadPriority;

        if (Prefs.Exist(Prefs.LOAD_LEVEL_NAME))
        {
            string scene = Prefs.Game_LevelName();
            LoadLevelAsync(scene);
        }
        else
        {
            SpinnerGO.GetComponentInChildren<Spinner>().isSpinning = false;
            Debug.LogError("Loading Error: There is no scene to load!");
        }

        if (FindObjectOfType<Camera>() != null)
        {
            MainCamera = FindObjectOfType<Camera>().gameObject;
        }
        else
        {
            MainCamera = null;
        }

    }

    private void Awake()
    {
        scenesStacks = new Dictionary<string, LoadStack>();
        foreach (var stack in sceneInfos)
            scenesStacks.Add(stack.Name, stack);
    }

    public void LoadLevelAsync(string stack)
    {
        if (scenesStacks.ContainsKey(stack))
        {
            var scene = scenesStacks[stack];
            sceneName.text = scene.Name;
            sceneDescription.text = scene.SceneDescription;
            if (scene.Background != null)
                backgroundImg.sprite = scene.Background;

            StartCoroutine(LoadScene(scene));
        }
        else
            Debug.LogError("This scene is does not exsists!");

    }

    IEnumerator LoadScene(LoadStack stack)
    {
        yield return new WaitForSeconds(timeBeforeLoad);

        AsyncOperation[] operations = new AsyncOperation[stack.Scenes.Length];

        for (int i = 0; i < operations.Length; i++)
            operations[i] = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(stack.Scenes[i], UnityEngine.SceneManagement.LoadSceneMode.Additive);


        bool done = false;
        SpinnerGO.SetActive(true);
        while (!done)
        {
            for (int i = 0; i < operations.Length; i++)
            {
                if (!operations[i].isDone)
                {
                    done = false;
                    yield return new WaitForEndOfFrame();
                    break;
                }
                done = true;
            }
        }


        SpinnerGO.SetActive(false);
        manuallySwitchText.SetActive(true);

        if (tipsManager)
        {
            tipsManager.TipsText.gameObject.SetActive(false);
        }

        yield return new WaitUntil(() => Input.anyKey);

        if (!fadeController)
        {
            if (MainCamera != null)
            {
                Destroy(MainCamera);
            }

            SceneTool.AllowSceneActivation();
        }
        else
        {
            fadeController.FadeInPanel();
            yield return new WaitUntil(() => !fadeController.isFading());

            if (MainCamera != null)
            {
                Destroy(MainCamera);
            }

            SceneTool.AllowSceneActivation();
        }

        HFPS_GameManager.Instance.OnSceneLoadedInvoke();
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("SceneLoader");
    }
}

