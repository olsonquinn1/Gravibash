using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using static ControllHandler;


public class MenuController : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private TMP_InputField ip_input;
    [SerializeField] private NetworkManager netMan;
    bool isSettingKey = true;

    void Start()
    {
        hostBtn.onClick.AddListener(onHost);
        joinBtn.onClick.AddListener(onJoin);
        quitBtn.onClick.AddListener(onQuit);

        StartControllSys();
    }

    private void onHost() {
        netMan.StartHost();
    }

    private void onJoin() {
        netMan.networkAddress = ip_input.text;
        netMan.StartClient();
    }

    private void onQuit() {
        Application.Quit();
    }
}
