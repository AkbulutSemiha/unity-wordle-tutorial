using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    private static readonly string[] SEPARATOR = new string[] { "\r\n", "\r", "\n" };
    public string targetWord; // Hedef kelime
    public string guess;
    public string[] solutions;
    public string[] validWords;

    private void Awake()
    {
        // Singleton yapısı
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Oyun sahneleri arasında saklanır
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTargetWord()
    {
        LoadData();
        targetWord = solutions[Random.Range(0, solutions.Length)];
        Debug.Log("Hedef kelime ayarlandı: " + targetWord);

        guess = solutions[Random.Range(0, solutions.Length)];
        Debug.Log("Tahmin kelime ayarlandı: " + guess);
    }

    private void LoadData()
    {
        //TextAsset textFile = Resources.Load("official_wordle_common") as TextAsset;
        //solutions = textFile.text.Split(SEPARATOR, System.StringSplitOptions.None);
        TextAsset textFile;
        textFile = Resources.Load("official_wordle_all") as TextAsset;
        validWords = textFile.text.Split(SEPARATOR, System.StringSplitOptions.None);
        solutions= validWords;
    }
}
