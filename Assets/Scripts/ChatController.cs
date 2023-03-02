using Mono.Cecil.Cil;
using OpenAI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ChatController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputQuestion;
    [SerializeField] private UnityEngine.UI.Button buttonSend;
    [SerializeField] private UnityEngine.UI.Button buttonNewPerson;
    [SerializeField] private TMP_Text answerAI;
    [SerializeField] private UnityEngine.UI.Image imagePerson;
    [SerializeField] private UnityEngine.UI.Image loadScreen;

    [SerializeField] private string dataPerson;
    private bool havePersonData;
    private bool haveImagePerson;

    //ENTER YOUR API KEY HERE:
    private OpenAIApi openAI = new OpenAIApi();

    private string askPrompt = "Act as a random person in a chat room and answer the questions.\nQ: Tell me who you are. Name, age, skin color, hair color, eye color.\nA: ";


    
    private void Awake()
    {
        dataPerson = null;
        havePersonData = false;
        GetDataPerson();
        haveImagePerson = false;

    }
    private void Start()
    {
        buttonSend.onClick.AddListener(GetReply);
        buttonNewPerson.onClick.AddListener(FindNewPerson);
        inputQuestion.ActivateInputField();
        loadScreen.gameObject.SetActive(true);

    }

    private void Update()
    {
        if (havePersonData && !haveImagePerson)
        {
            CreatedImageRequest();
            haveImagePerson = true;
        }

        if (Input.GetKeyDown(KeyCode.Return)) { GetReply(); }
        if(Input.GetKeyDown(KeyCode.Escape)) { FindNewPerson(); }
    }

    public void GetReply()
    {
        SendReply();
    }
    public void GetDataPerson()
    {
        SendReply();
    }
    public void FindNewPerson()
    {
        SceneManager.LoadScene("ChatScene");
    }

    //Chat-GPT
    private async void SendReply()
    {
        
        askPrompt += $"{inputQuestion.text}\nA: ";

        inputQuestion.text = ""; 
        answerAI.text = "...";

        buttonSend.enabled = false;
        inputQuestion.enabled = false;

        //Interaction with OpenAI
        var questionAnswer = await openAI.CreateCompletion(new CreateCompletionRequest()
        {
            Prompt= askPrompt,
            Model = "text-davinci-003",
            MaxTokens=100,
        });

        if(questionAnswer.Choices != null && questionAnswer.Choices.Count > 0)
        {
            if (dataPerson== null)
            { 
                dataPerson = questionAnswer.Choices[0].Text;
                askPrompt = $"{questionAnswer.Choices[0].Text}\nQ: ";
                havePersonData = true;
            }
            else 
            {
                answerAI.text = questionAnswer.Choices[0].Text;
                askPrompt = $"{questionAnswer.Choices[0].Text}\nQ: ";
            }
        } else
        {
            Debug.LogWarning("ERROR");
        }

        buttonSend.enabled = true;
        inputQuestion.enabled = true;
        inputQuestion.ActivateInputField();
        
    }

    //Dall-E
    private async void CreatedImageRequest()
    {
        var response = await openAI.CreateImage(new CreateImageRequest
        {
            Prompt = dataPerson,
            Size = ImageSize.Size256
        });

        if (response.Data != null && response.Data.Count > 0)
        {
            using (var request = new UnityWebRequest(response.Data[0].Url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                request.SendWebRequest();

                while (!request.isDone) await Task.Yield();

                Texture2D texture = new Texture2D(256, 256);
                texture.LoadImage(request.downloadHandler.data);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 1f);
                imagePerson.sprite = sprite;
                   
            }
        }
        else
        {
            Debug.LogWarning("No image was created from this prompt.");
        }

        loadScreen.gameObject.SetActive(false);
    }

}
