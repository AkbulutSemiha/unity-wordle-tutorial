using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;


[System.Serializable]  
public class Guess  
{  
    public string guess;  
    public List<int> feedback;  
}  

[System.Serializable]  
public class Guesses  
{  
    public List<Guess> guesses;  
}  
[System.Serializable]  
public class PredictionResponse  
{  
    public string prediction;  
}  
[DefaultExecutionOrder(-1)] 
public class yz : MonoBehaviour
{
    private static readonly string[] SEPARATOR = new string[] { "\r\n", "\r", "\n" };

    private Row[] rows;
    private int rowIndex;
    private int columnIndex;

    public string[] solutions;
    public string[] validWords;
    public string word;
    public string guess;
    Guesses guesses = new Guesses  
    {  
        guesses = new List<Guess>()  
    };  
    [Header("Tiles")]
    public Tile.State emptyState;
    public Tile.State occupiedState;
    public Tile.State correctState;
    public Tile.State wrongSpotState;
    public Tile.State incorrectState;

    [Header("UI")]
    public GameObject tryAgainButton;
    public GameObject newWordButton;
    public GameObject invalidWordText;

    private void Awake()
    {
        rows = GetComponentsInChildren<Row>();
    }

    private void Start()
    {
        NewGame();
        StartProcessing();
    }
    void AddGuess(Guesses guesses, string guessWord, List<int> feedback)  
    {  
        Guess newGuess = new Guess  
        {  
            guess = guessWord,  
            feedback = feedback  
        };  
        guesses.guesses.Add(newGuess);  
    }  
    IEnumerator PostDataToFastAPI()
    {
        Debug.Log("PostDataToFastAPI");  
        // Gönderilecek veri (JSON formatında)  
        string jsonData = JsonConvert.SerializeObject(guesses, Formatting.Indented);   
        Debug.Log("Gönderilen JSON: " + jsonData);  

        // UnityWebRequest ile HTTP isteği oluştur  
        using (UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/postfeedbacklstm/", "POST"))  
        {  
            // JSON verisini gönderilecek veri olarak ayarla  
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);  
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);  
            request.downloadHandler = new DownloadHandlerBuffer();  
            request.SetRequestHeader("Content-Type", "application/json");  

            // İsteği gönder ve yanıtı bekle  
            yield return request.SendWebRequest();  

            // Yanıt kontrolü  
            if (request.result == UnityWebRequest.Result.Success)  
            {  
                Debug.Log("Başarılı yanıt: " + request.downloadHandler.text);  
                // Yanıtı işleme  
                ProcessResponse(request.downloadHandler.text);  
            }  
            else  
            {  
                Debug.LogError("Hata oluştu: " + request.error);  
            }  
        } 
    }
    private void ProcessResponse(string jsonResponse)  
    {  
        // Yanıtı işleyin (örneğin, tahmin kelimesini alın)  
        var predictionResponse = JsonConvert.DeserializeObject<PredictionResponse>(jsonResponse);  
        Debug.Log("Tahmin: " + predictionResponse.prediction);  
        guess = predictionResponse.prediction;
        PerformOtherOperations(predictionResponse.prediction);  
    }  

    private void PerformOtherOperations(string prediction)  
    {  
        // Yanıt alındıktan sonra yapılacak diğer işlemler  
        Debug.Log("Performing other operations with prediction: " + prediction);  
        guess = prediction;

    } 

    public void NewGame()
    {
        ClearBoard();
        ClearBoard();
        if (string.IsNullOrEmpty(DataManager.Instance.targetWord))
        {
            // Eğer hedef kelime belirlenmemişse, burada belirle
            DataManager.Instance.SetTargetWord();
        }

        // Hedef kelimeyi al
        solutions = DataManager.Instance.solutions;
        validWords = DataManager.Instance.validWords;
        word = DataManager.Instance.targetWord;
        guess = DataManager.Instance.guess;
        Debug.Log("Tahmin kelimesi: " + guess);
        
        enabled = true;
        enabled = true;
    }

    public void TryAgain()
    {
        ClearBoard();
        NewGame();
        StartProcessing();
        enabled = true;
    }

    private IEnumerator ProcessGuess()  
    {  
        Row currentRow = rows[rowIndex];  
        if (columnIndex < guess.Length)  
        {  
            char letter = guess[columnIndex];  
            currentRow.tiles[columnIndex].SetLetter(letter);  
            currentRow.tiles[columnIndex].SetState(occupiedState);  
            columnIndex++;  
        }  
        else  
        {  
            Debug.Log("SubmitRow");  
            AddGuess(guesses, guess, SubmitRow(currentRow));  
            if (string.Equals(guess, word, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Hedef kelime tahmin edildi: " + guess);
                enabled = false; // Tahmin işlemine devam etme
                yield break; // Coroutine'den çık
            }
            yield return StartCoroutine(PostDataToFastAPI()); // POST isteğini bekle  

            rowIndex++; // Bir sonraki satıra geç  
            columnIndex = 0; // Kolon indeksini sıfırla  

            if (rowIndex >= rows.Length)  
            {  
                enabled = false;  
            }  

        }  

        yield return new WaitForSeconds(0.5f); // 0.5 saniye bekle  

        if (rowIndex < rows.Length)  
        {  
            StartCoroutine(ProcessGuess());  
        }  
    } 

    private void StartProcessing()  
    { 
        Debug.Log("startprocess");
        StartCoroutine(ProcessGuess());  
    }  

    private List<int> SubmitRow(Row row)
    {
        List<int> feedback = new List<int>();
        Debug.Log("tiles len: "+row.tiles.Length);


        string remaining = word;
       
        // Check correct/incorrect letters first
        for (int i = 0; i < row.tiles.Length; i++)
        {
            Tile tile = row.tiles[i];

            if (tile.letter == word[i])
            {
                feedback.Add(2);
                tile.SetState(correctState);

                remaining = remaining.Remove(i, 1);
                remaining = remaining.Insert(i, " ");
            }
            else if (!word.Contains(tile.letter))
            {
                feedback.Add(0);
                tile.SetState(incorrectState);
            }
            else
            {
                feedback.Add(1);
            }
        }

        // Check wrong spots after
        for (int i = 0; i < row.tiles.Length; i++)
        {
            Tile tile = row.tiles[i];

            if (tile.state != correctState && tile.state != incorrectState)
            {
                if (remaining.Contains(tile.letter))
                {
                    tile.SetState(wrongSpotState);

                    int index = remaining.IndexOf(tile.letter);
                    remaining = remaining.Remove(index, 1);
                    remaining = remaining.Insert(index, " ");
                }
                else
                {
                    tile.SetState(incorrectState);
                }
            }
        }

        if (HasWon(row)) {
            enabled = false;
        }

        //rowIndex++;
        columnIndex = 0;

        if (rowIndex >= rows.Length) {
            enabled = false;
        }
        return feedback;
    }



    private bool HasWon(Row row)
    {
        for (int i = 0; i < row.tiles.Length; i++)
        {
            if (row.tiles[i].state != correctState) {
                return false;
            }
        }

        return true;
    }

    private void ClearBoard()
    {
        for (int row = 0; row < rows.Length; row++)
        {
            for (int col = 0; col < rows[row].tiles.Length; col++)
            {
                rows[row].tiles[col].SetLetter('\0');
                rows[row].tiles[col].SetState(emptyState);
            }
        }

        rowIndex = 0;
        columnIndex = 0;
    }

    private void OnEnable()
    {
        tryAgainButton.SetActive(false);
        newWordButton.SetActive(false);
    }

    private void OnDisable()
    {
        tryAgainButton.SetActive(true);
        newWordButton.SetActive(true);
    }

}
