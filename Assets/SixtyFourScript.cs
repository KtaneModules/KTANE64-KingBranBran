using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SixtyFourScript : MonoBehaviour {

    public KMSelectable[] buttons;
    public KMBombModule module;
    public KMAudio moduleAudio;

    public GameObject display;
    public GameObject screenTextGameObject;
    public TextMesh screenText;
    
    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private int number;
    private string numberIn64;
    private string numberInBinary;

    private bool buttonMoving = false;
    private string currentInput = "";
    private bool holding = false;
    private Coroutine holdCoro;

    private bool submitting = false;
    private bool solved = false;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;

        buttons[0].OnInteract += delegate { ButtonPressed(0); return false; };
        buttons[1].OnInteract += delegate { ButtonPressed(1); return false; };

        buttons[0].OnInteractEnded += delegate { ButtonReleased(0); };
        buttons[1].OnInteractEnded += delegate { ButtonReleased(1); };
    }

	void Start()
	{
        GenerateNumber();
    }


    void ButtonPressed(int buttonNum)
    {
        StartCoroutine(ButtonMove(buttonNum, "down"));
        moduleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, buttons[buttonNum].transform);
        buttons[buttonNum].AddInteractionPunch();
        if (solved || submitting) return;

        if (holdCoro != null)
        {
            holding = false;
            StopCoroutine(holdCoro);
            holdCoro = null;
        }

        holdCoro = StartCoroutine(HoldChecker());
    }

    IEnumerator HoldChecker()
    {
        yield return new WaitForSeconds(.6f);
        holding = true;
        screenText.text = "";
    }

    void ButtonReleased(int buttonNum)
    {   
        StartCoroutine(ButtonMove(buttonNum, "up"));
        moduleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, buttons[buttonNum].transform);
        StopCoroutine(holdCoro);

        if (solved || submitting) return;

        if (holding)
        {
            if (buttonNum == 1)
            {
                StartCoroutine(StartGlitchySubmit());
            }
            else
            {
                currentInput = "";
                screenText.text = numberIn64;
            }
        }
        else
        {
            currentInput += buttonNum;
        }
    }

    IEnumerator ButtonMove(int buttonNum, string direction)
    {
        yield return new WaitUntil(() => !buttonMoving);
        buttonMoving = true;

        var buttonToMove = buttons[buttonNum].transform;
        var x = buttonToMove.localPosition.x;
        var z = buttonToMove.localPosition.z;

        switch (direction)
        {
            case "down":
                buttonToMove.localPosition = new Vector3(x, 0.009f, z);
                break;
            case "up":
                buttonToMove.localPosition = new Vector3(x, 0.01f, z);
                yield return new WaitForSeconds(.01f);
                buttonToMove.localPosition = new Vector3(x, 0.012f, z);
                yield return new WaitForSeconds(.01f);
                buttonToMove.localPosition = new Vector3(x, 0.014f, z);
                yield return new WaitForSeconds(.01f);
                buttonToMove.localPosition = new Vector3(x, 0.016f, z);
                break;
        }

        buttonMoving = false;
    }

    void GenerateNumber()
    {
        number = Random.Range(0, 16777216);
        numberIn64 = DecimalToArbitrarySystem(number, 64);
        numberInBinary = DecimalToArbitrarySystem(number, 2);

        screenText.text = numberIn64;

        DebugLog("The displayed number is {0}, which is {1} in decimal.", numberIn64, number);
        DebugLog("The number in binary is {0}.", numberInBinary);
    }

    void SubmitAnswer()
    {
        StopAllCoroutines();
        screenText.transform.localPosition = new Vector3(0, 0, .0011f);

        if (currentInput == numberInBinary)
        {
            module.HandlePass();
            DebugLog("You submitted {0}. That is correct!", currentInput);
            DebugLog("Module solved!");
            screenText.text = "correct";
            moduleAudio.PlaySoundAtTransform("Solve", module.transform);
        }
        else
        {
            module.HandleStrike();
            DebugLog("You submitted {0}. That is wrong...", currentInput);
            StartCoroutine(StrikeAnimation());
            
        }
    }

    IEnumerator StartGlitchySubmit()
    {
        submitting = true;

        var submitAudio = moduleAudio.PlaySoundAtTransformWithRef("Submit", module.transform);
        StartCoroutine(TextGlitch());
        StartCoroutine(CycleNumbers());
        yield return new WaitForSeconds(5f);
        submitAudio.StopSound();
        submitting = false;

        SubmitAnswer();      
    }

    IEnumerator StrikeAnimation()
    {
        submitting = true;

        moduleAudio.PlaySoundAtTransform("Strike", module.transform);
        StartCoroutine(DoCrazyStrikeStuff());
        yield return new WaitForSeconds(1.352f);
        currentInput = "";
        GenerateNumber();

        submitting = false;
    }

    IEnumerator DoCrazyStrikeStuff()
    {
        List<GameObject> instantiatedText = new List<GameObject>();
        var possibleText = "x!*?-/";

        screenText.text = "";

        while (submitting)
        {
            var x = Random.Range(0f, .0008f);
            var y = Random.Range(0f, .0005f);

            x = Random.Range(0, 2) == 0 ? x *= -1 : x;
            y = Random.Range(0, 2) == 0 ? y *= -1 : y;

            var newTextMesh = Instantiate(screenTextGameObject, display.transform);
            instantiatedText.Add(newTextMesh);

            newTextMesh.transform.localPosition = new Vector3(x, y, .0011f);
            newTextMesh.GetComponent<TextMesh>().color = Color.red;
            newTextMesh.GetComponent<TextMesh>().text = possibleText[Random.Range(0, possibleText.Length)].ToString();
            yield return new WaitForSeconds(.05f);
        }

        foreach (GameObject obj in instantiatedText)
        {
            Destroy(obj);
        }

        screenText.transform.localPosition = new Vector3(0, 0, .0011f);
    }

    IEnumerator CycleNumbers()
    {
        while (submitting)
        {
            var tempNumber = Random.Range(0, 16777216);
            var tempNumberIn64 = DecimalToArbitrarySystem(tempNumber, 64);
            screenText.text = tempNumberIn64;

            yield return new WaitForSeconds(.0001f);
        } 
    }

    IEnumerator TextGlitch()
    {
        float maxAmountX = 0;

        var amountX = 0f;
        var amountY = 0f;
        var maxAmountY = .0005f;

        while (submitting)
        {
            // Find the amount the message can move using the amount of 'W's.
            switch (numberIn64.Where(ix => "Ww".Contains(ix.ToString())).Count())
            {
                case 0:
                    maxAmountX = .0003f;
                    break;
                case 1:
                    maxAmountX = .0002f;
                    break;
                case 2:
                    maxAmountX = .00015f;
                    break;
                case 3:
                    maxAmountX = .0001f;
                    break;
                case 4:
                    maxAmountX = .0003f;
                    break;
            }

            // Adds some to the amount if there are smaller characters.
            switch (numberIn64.Where(ix => "iljt1I/".Contains(ix.ToString())).Count())
            {
                case 0:
                    maxAmountX += 0f;
                    break;
                case 1:
                    maxAmountX += .00015f;
                    break;
                case 2:
                    maxAmountX += .0002f;
                    break;
                case 3:
                    maxAmountX = .0002f;
                    break;
                case 4:
                    maxAmountX = .00025f;
                    break;
            }

            var x = Random.Range(0, amountX);
            var y = Random.Range(0, amountY);
            screenText.transform.localPosition = new Vector3(x, y, .0011f);

            // Only increment if the coordinates are smaller than its max.
            amountX = amountX >= maxAmountX ? amountX : amountX + .0001f / 5f; 
            amountY = amountY >= maxAmountY ? amountY : amountY + .0001f / 5f;

            // Randomly multiply by -1.
            amountX = Random.Range(0, 2) == 0 ? amountX *= -1 : amountX;
            amountY = Random.Range(0, 2) == 0 ? amountY *= -1 : amountY;

            yield return new WaitForSeconds(.0001f);
        }
    }

    public static string DecimalToArbitrarySystem(long decimalNumber, int radix)
    {
        const int BitsInLong = 64;
        string Digits = radix == 64 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/" : "01";

        if (radix < 2 || radix > Digits.Length)
            throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());

        if (decimalNumber == 0)
            return "0";

        int index = BitsInLong - 1;
        long currentNumber = Math.Abs(decimalNumber);
        char[] charArray = new char[BitsInLong];

        while (currentNumber != 0)
        {
            int remainder = (int)(currentNumber % radix);
            charArray[index--] = Digits[remainder];
            currentNumber = currentNumber / radix;
        }

        string result = new String(charArray, index + 1, BitsInLong - index - 1);
        if (decimalNumber < 0)
        {
            result = "-" + result;
        }

        return result;
    }

    private void DebugLog(string log, params object[] args)
    {
        var logData = string.Format(log, args);
        Debug.LogFormat("[64 #{0}] {1}", _moduleId, logData);
    }

    string TwitchHelpMessage = "Use '!{0} reset left 0 1 10101 submit' to reset the module, press the buttons, and submit. The first letter of the reset and submit commands can be used also.";

    int TwitchModuleScore = 9;
    IEnumerator ProcessTwitchCommand(string command)
    {
        var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.All(x => new[] {"reset", "submit", "r", "s"}.Contains(x) || x.All(c => "01".Contains(c.ToString())))) {

            yield return null;

            for (int i = 0; i < parts.Length; i++)
            {
                

                var part = parts[i];

                if (part == "reset" || part == "r")
                {
                    yield return "trycancel";
                    ButtonPressed(0);
                    yield return new WaitForSeconds(.7f);
                    ButtonReleased(0);
                }
                else if (part == "submit" || part == "s")
                {
                    yield return "trycancel";
                    ButtonPressed(1);
                    yield return new WaitForSeconds(.7f);
                    ButtonReleased(1);
                    if (currentInput == numberInBinary) yield return "solve"; else yield return "strike";
                }
                else
                {
                    for (int ix = 0; ix < part.Length; ix++)
                    {
                        yield return "trycancel";
                        if (part[ix] == '1')
                        {
                            ButtonPressed(1);
                            ButtonReleased(1);
                        }
                        else
                        {
                            ButtonPressed(0);
                            ButtonReleased(0);
                        }
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }
        }
    }
}
