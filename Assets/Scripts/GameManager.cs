using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour {

    private const int DefaultDelayBeforeNewRound = 1000; //ms
    private const int MinTimeBeforeButtonColorChange = 1000; //ms
    private const int MaxTimeBeforeButtonColorChange = 3000; //ms

    //Variables could be visible in the editor instead of constants to allow designers to change them.
    //[SerializeField]
    //private int DefaultDelayBeforeNewRound = 1000;
    //[SerializeField]
    //private int MinTimeBeforeButtonColorChange = 1000;
    //[SerializeField]
    //private int MaxTimeBeforeButtonColorChange = 3000;

    private bool canStartGame = true;

    [SerializeField]
    private TMP_Text gameStateText;

    [SerializeField]
    private Player[] players;

    private IEnumerable<Button> getButtons() {
        for (int i = 0; i < players.Length; i++) {
            yield return players[i].myButton;
        }
    }

    void Start() {
        //Reseting points for players.
        foreach (Player player in players) {
            player.CurrentScore = 0;
            player.UpdateScores();
        }

        SetRandomColorForButtons();
        gameStateText.text = GameText.StartGameText;
    }

    public void StartGame() {
        //canStartGame boolean prevents player from pressing start game button multiple times.
        if (canStartGame) {
            canStartGame = false;
            NewRound();
        }
    }

    private async void NewRound() {
        gameStateText.text = GameText.WaitText;

        bool buttonPressedTooEarly = true;
        int randomTime = Random.Range(MinTimeBeforeButtonColorChange, MaxTimeBeforeButtonColorChange);

        Task<int> waitForButtonInputTask = SimplifiedMysteryMethod(getButtons());
        Task delayTask = Task.Delay(randomTime);

        await Task.WhenAny(waitForButtonInputTask, delayTask);

        //Change color of all the buttons if a button was not pressed before delay.
        if (delayTask.IsCompleted) {

//This prevent task running after exiting from playmode in the editor.
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            SetRandomColorForButtons();

            buttonPressedTooEarly = false;
            gameStateText.text = GameText.PressButtonText;
        }

        //Wait for button press.
        await waitForButtonInputTask;

        int playerIndex = waitForButtonInputTask.Result;

        //If a button is pressed at the wrong time, reduce point from the player and give point to other players.
        if (buttonPressedTooEarly) {
            for (int i = 0; i < players.Length; i++) {
                if (i == playerIndex) {
                    players[i].CurrentScore--;
                }
                else {
                    players[i].CurrentScore++;
                }

                players[i].UpdateScores();
            }

            gameStateText.text = GameText.WrongTimeText;

        }
        //If a button is pressed at the correct time, give the player a point.
        else {
            players[playerIndex].CurrentScore++;
            players[playerIndex].UpdateScores();

            gameStateText.text = GameText.GoodJobText;

        }

        //Removing all OnClick events from buttons.
        foreach (Player player in players) {
            player.myButton.onClick.RemoveAllListeners();
        }


        //Delay that allows players to see game state message.
        await Task.Delay(DefaultDelayBeforeNewRound);

        NewRound();
    }

    //Provided code. 
    static async Task<int> MysteryMethod(IEnumerable<Button> buttons) {
        var taskCompletionSource = new TaskCompletionSource<int>();
        foreach (var pair in System.Linq.Enumerable.Select(buttons, (button, index) => new { button, index }))
            pair.button.onClick.AddListener(() => taskCompletionSource.SetResult(pair.index));

        return await taskCompletionSource.Task;
    }


    //In this case code could be simplified since mystery method does not need to be async as it does not contain other async methods.
    private Task<int> SimplifiedMysteryMethod(IEnumerable<Button> buttons) {
        var taskCompletionSource = new TaskCompletionSource<int>();
        foreach (var pair in System.Linq.Enumerable.Select(buttons, (button, index) => new { button, index }))
            pair.button.onClick.AddListener(() => taskCompletionSource.SetResult(pair.index));

        return taskCompletionSource.Task;

    }

    private void SetRandomColorForButtons() {
        foreach (Player player in players) {
            player.myButton.image.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }
    }


}

[System.Serializable]
public class Player {
    public TMP_Text myScoreText;
    public Button myButton;

    public int CurrentScore { get; set; }

    public void UpdateScores() {
        myScoreText.text = GameText.ScoreText + CurrentScore;
    }
}


public struct GameText {
    public const string StartGameText = "Start game!";
    public const string WaitText = "Wait!";
    public const string PressButtonText = "Press button!";
    public const string GoodJobText = "Great job!";
    public const string WrongTimeText = "Wrong time!";
    public const string ScoreText = "Score: ";
}