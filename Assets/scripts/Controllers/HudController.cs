using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudController : MonoBehaviour
{
    [SerializeField] Canvas nameChangeCanvas;
    [SerializeField] public TMP_Text nameText;
    [SerializeField] Button nameChangeButton;

    public Player playerScript;
    
    void Start()
    {
        nameChangeButton.onClick.AddListener(nameChangeButtonOnClick);
    }

    public void showNameChangeHud() {
        nameChangeCanvas.gameObject.SetActive(true);
    }

    void nameChangeButtonOnClick() {
        playerScript.changePlayerName(nameText.text);
        nameChangeCanvas.gameObject.SetActive(false);
    }

}
