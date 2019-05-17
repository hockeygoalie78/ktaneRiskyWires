using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

/// <summary>
/// Module created by hockeygoalie78
/// Gamble for the chance of an easier module, then cut the wires necessary.
/// </summary>
public class RiskyWires : MonoBehaviour
{
    public KMBombInfo bombInfo;
    public KMAudio bombAudio;
    public Material[] wireMaterials; //0 is red, 1 is blue, 2 is yellow, 3 is green, 4 is purple
    public Material[] cutWireMaterials; //0 is red, 1 is blue, 2 is yellow, 3 is green, 4 is purple
    public Material[] LEDMaterials; //0 is black, 1 is red, 2 is green
    public MeshRenderer[] wireRenderers;
    public MeshRenderer[] cutWireRenderers;
    public MeshRenderer[] LEDRenderers; //0 is top, 1 is bottom
    public List<KMSelectable> wires;
    public GameObject[] highlights;
    public Animator[] doorAnimators;
    public KMSelectable riskyWires;
    public KMSelectable recessedInner;
    public KMSelectable gambleButton;
    public KMSelectable revealButton;
    public KMSelectable submitButton;
    public TextMesh wireCountDisplay;
    public TextMesh gambleOddsDisplay;
    public TextMesh IDDisplay;
    private KMSelectable[] updatedChildren;
    private int[] wireMaterialIndices;
    private int[] digits;
    private int[] LEDMaterialIndices;
    private KMBombModule bombModule;

    private int wireCount;
    private const int MAX_WIRES = 8;
    private int gambleAttempt;
    private int oddsPercentage;
    private bool wiresRevealed;
    private bool moduleSolved;
    private int randomInt;
    private int[] odds;
    private bool specialRules6;
    private bool specialRules8;
    private Dictionary<int, bool> wiresToCut;
    private bool wireCutNecessary;
    private string cutString;

    private string serialNumber;
    private bool serialLastDigitEven;
    private bool serialVowel;
    private bool serialNumberSpecial; //Serial number contains 2 or 5
    private bool serialLetterSpecial; //4 letters in the serial number
    private int digitSum; //Sum of the ID number's digits is less than 17
    private bool numberSpecial; //3 in either the serial number or ID number
    private int litIndicatorCount;
    private int unlitIndicatorCount;
    private bool containsSpecificIndicator6Wires; //Contains lit indicator FRK or BOB
    private bool containsSpecificIndicator8Wires; //Contains unlit indicator SND or NSA
    private int aaBatteryCount;
    private int dBatteryCount;
    private int batteryCount;
    private int evenDigitCount; //Number of even digits in the ID number
    private bool containsSpecificPorts5Wires; //Contains a Serial port but no red LEDs
    private bool containsSpecificPorts8Wires; //Contains PS/2, Stereo RCA, or RJ-45 port
    private bool containsDuplicatePort;

    private static int moduleIdCounter = 1;
    private int moduleId;

    void Start ()
    {
        //Set module ID
        moduleId = moduleIdCounter++;

        //Initialize the colors of the wires
        wireMaterialIndices = new int[MAX_WIRES];
        for(int c = 0; c < MAX_WIRES; c++)
        {
            randomInt = Random.Range(0, wireMaterials.Length);
            wireMaterialIndices[c] = randomInt;
            wireRenderers[c].material = wireMaterials[randomInt];
            cutWireRenderers[c].material = cutWireMaterials[randomInt];
        }

        //Initialize ID number
        digits = new int[4];
        for(int c = 0; c < 4; c++)
        {
            digits[c] = Random.Range(0, 10);
        }
        IDDisplay.text = digits[0].ToString() + digits[1].ToString() + digits[2].ToString() + digits[3].ToString();

        //Initialize LEDs
        LEDMaterialIndices = new int[2];
        for(int c = 0; c < 2; c++)
        {
            randomInt = Random.Range(0, 3);
            LEDRenderers[c].material = LEDMaterials[randomInt];
            LEDMaterialIndices[c] = randomInt;
        }

        //Initialize other values
        wireCount = 6;
        odds = new int[6] { 90, 80, 65, 50, 35, 20 };
        oddsPercentage = odds[0];
        gambleAttempt = 1;
        wiresRevealed = false;
        moduleSolved = false;
        bombModule = GetComponent<KMBombModule>();
        updatedChildren = new KMSelectable[6] { recessedInner, gambleButton, recessedInner, revealButton, recessedInner, submitButton };
        specialRules6 = false;
        specialRules8 = false;

        //Set up button interactions
        gambleButton.OnInteract += delegate { Gamble(); return false; };
        revealButton.OnInteract += delegate { RevealWires(); return false; };
        submitButton.OnInteract += delegate { CheckSolution(); return false; };

        //Serial number
        serialNumber = bombInfo.GetSerialNumber();
        serialLastDigitEven = int.Parse(serialNumber.Substring(5)) % 2 == 0;
        serialVowel = serialNumber.Any("AEIOU".Contains);
        serialLetterSpecial = bombInfo.GetSerialNumberLetters().Count() >= 4;
        serialNumberSpecial = serialNumber.IndexOf("2") >= 0 || serialNumber.IndexOf("5") >= 0;

        //Sum of the ID number's digits is less than 17
        digitSum = digits.Sum();

        //3 in either the serial number or ID number
        numberSpecial = false;
        IEnumerable<int> numbers = bombInfo.GetSerialNumberNumbers();
        foreach(int digit in numbers)
        {
            if(digit == 3)
            {
                numberSpecial = true;
                break;
            }
        }
        if(!numberSpecial)
        {
            for(int c = 0; c < digits.Length; c++)
            {
                if(digits[c] == 3)
                {
                    numberSpecial = true;
                    break;
                }
            }
        }

        //Indicator counts
        litIndicatorCount = bombInfo.GetOnIndicators().Count();
        unlitIndicatorCount = bombInfo.GetOffIndicators().Count();

        //Number of even digits in the ID number
        evenDigitCount = 0;
        for(int c = 0; c < digits.Length; c++)
        {
            if(digits[c] % 2 == 0)
            {
                evenDigitCount++;
            }
        }

        //Contains lit indicator FRK or BOB
        containsSpecificIndicator6Wires = bombInfo.IsIndicatorOn("FRK") || bombInfo.IsIndicatorOn("BOB");

        //Contains unlit indicator SND or NSA
        containsSpecificIndicator8Wires = bombInfo.IsIndicatorOff("SND") || bombInfo.IsIndicatorOff("NSA");

        //Battery counts
        aaBatteryCount = bombInfo.GetBatteryCount(2) + bombInfo.GetBatteryCount(3) + bombInfo.GetBatteryCount(4);
        dBatteryCount = bombInfo.GetBatteryCount(1);
        batteryCount = aaBatteryCount + dBatteryCount;

        //Contains a Serial port but no red LEDs
        containsSpecificPorts5Wires = bombInfo.IsPortPresent("Serial") && LEDMaterialIndices[0] != 1 && LEDMaterialIndices[1] != 1;

        //Contains PS/2, Stereo RCA, or RJ-45 port
        containsSpecificPorts8Wires = bombInfo.IsPortPresent("PS2") || bombInfo.IsPortPresent("StereoRCA") || bombInfo.IsPortPresent("RJ45");

        //Contains duplicate ports
        containsDuplicatePort = bombInfo.IsDuplicatePortPresent();
    }

    /// <summary>
    /// Attempt a gamble, and handle either a successful or failed gamble
    /// </summary>
    private void Gamble()
    {
        //Audio and interaction
        gambleButton.AddInteractionPunch(.2f);
        bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        //Ignore the method call if the wires are already revealed
        if(wiresRevealed)
        {
            return;
        }

        //Check if gamble is successful and update properly
        randomInt = Random.Range(1, 101);
        if(randomInt <= oddsPercentage)
        {
            wireCount--;
            if(wireCount != 0)
            {
                Debug.LogFormat(@"[Risky Wires #{0}] Gamble attempt {1} was successful. Wire count is now {2}.", moduleId, gambleAttempt, wireCount);
                oddsPercentage = odds[gambleAttempt];
            }
            else
            {
                Debug.LogFormat(@"[Risky Wires #{0}] All gamble attempts were successful. Module passed, lucky.", moduleId);
                bombModule.HandlePass();
                //Doors should stay closed if gambled to success
                wiresRevealed = true;
                moduleSolved = true;
                oddsPercentage = 0;
            }
            gambleAttempt++;
        }
        else
        {
            Debug.LogFormat(@"[Risky Wires #{0}] Gamble attempt {1} failed. Revealing 8 wires.", moduleId, gambleAttempt);
            wireCount = MAX_WIRES;
            oddsPercentage = 0;
            RevealWires(false);
        }

        //Update displays
        wireCountDisplay.text = "0" + wireCount.ToString();
        if(oddsPercentage == 0)
        {
            gambleOddsDisplay.text = "00";
        }
        else
        {
            gambleOddsDisplay.text = oddsPercentage.ToString();
        }
    }

    /// <summary>
    /// Reveal the wires on the module
    /// </summary>
    /// <param name="buttonPress">True (by default) if the reveal button was pressed to trigger this method; false otherwise.</param>
    private void RevealWires(bool buttonPress = true)
    {
        //Audio and interaction
        if(buttonPress)
        {
            if(!wiresRevealed)
            {
                Debug.LogFormat(@"[Risky Wires #{0}] Reveal button pressed. Revealing {1} wires.", moduleId, wireCount);
            }

            revealButton.AddInteractionPunch(.2f);
            bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        }

        //Ignore the method call if the wires are already revealed
        if(wiresRevealed)
        {
            return;
        }

        //Door open audio
        bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        //Open doors
        doorAnimators[0].SetTrigger("OpenDoor");
        doorAnimators[1].SetTrigger("OpenDoor");

        //Hide the excess wires and remove those wires from the lists
        for(int c = wireCount; c < wireRenderers.Length; c++)
        {
            wireRenderers[c].enabled = false;
            highlights[c].SetActive(false);
        }
        wires.RemoveRange(wireCount, MAX_WIRES - wireCount);

        //Enable interaction of wires
        for(int c = 0; c < wires.Count; c++)
        {
            int d = c;
            wires[c].OnInteract += delegate { CutWire(d); return false; };
        }

        //Special rule for 6 wires
        //If both LEDs are red, the wires must be cut in reverse order (i.e. Wire 1 follows Wire 6's rules, Wire 6 follows Wire 1's rules, etc.).
        if(wireCount == 6 && LEDMaterialIndices[0] == 1 && LEDMaterialIndices[1] == 1)
        {
            Debug.LogFormat(@"[Risky Wires #{0}] The special rule set for 6 wires is applicable.", moduleId);
            specialRules6 = true;
        }
        //Special rule for 8 wires
        //If the ID number starts with a 4 or 7, the wire rules shift down one wire (i.e. Wire 2 follows Wire 1's rules); Wire 1 follows Wire 8's rules.
        else if(wireCount == 8 && (digits[0] == 4 || digits[0] == 7))
        {
            Debug.LogFormat(@"[Risky Wires #{0}] The special rule set for 8 wires is applicable.", moduleId);
            specialRules8 = true;
        }

        //Update children of module's main KMSelectable
        riskyWires.Children = updatedChildren;
        riskyWires.UpdateChildren();

        //Prevent more calls of reveal method
        wiresRevealed = true;

        //Set the dictionary for which wires should be cut
        wiresToCut = new Dictionary<int, bool>();
        for(int c = 0; c < wires.Count; c++)
        {
            wireCutNecessary = WireShouldBeCut(c);
            cutString = wireCutNecessary ? "" : "not ";
            Debug.LogFormat(@"[Risky Wires #{0}] Wire {1} should {2}be cut.", moduleId, c + 1, cutString);
            wiresToCut.Add(c, wireCutNecessary);
        }
    }

    /// <summary>
    /// Cut the wire at the index
    /// </summary>
    /// <param name="index">The index of the wire to be cut</param>
    private void CutWire(int index)
    {
        //If the wire is already cut, ignore the method call
        if(cutWireRenderers[index].enabled)
        {
            return;
        }

        //Audio
        bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, transform);

        //Update wire to cut wire
        wireRenderers[index].enabled = false;
        cutWireRenderers[index].enabled = true;
        Debug.LogFormat(@"[Risky Wires #{0}] Wire {1} cut.", moduleId, index + 1);

        //If this wire should not have been cut, add a strike
        if (!wiresToCut[index])
        {
            Debug.LogFormat(@"[Risky Wires #{0}] Wire {1} should not have been cut. Strike occurred.", moduleId, index + 1);
            bombModule.HandleStrike();
        }
    }

    /// <summary>
    /// Indicates whether or not a wire should have been cut based on the rules of the manual
    /// </summary>
    /// <param name="index">The index of the wire in the list</param>
    /// <returns>True if the wire should be cut; false otherwise</returns>
    private bool WireShouldBeCut(int index)
    {
        //Special rules effect
        if(specialRules6)
        {
            index = 5 - index;
        }
        else if(specialRules8)
        {
            index = (index + 7) % 8;
        }

        //Find which wire was cut, then determine the subset rule based on the wire count
        switch(index)
        {
            //First wire
            case 0:
                switch(wireCount)
                {
                    //If this wire is red or yellow, cut the wire.
                    case 1:
                        return wireMaterialIndices[0] == 0 || wireMaterialIndices[0] == 2;
                    //If the serial number has a vowel, cut the wire.
                    case 2:
                        return serialVowel;
                    //If any of the other wires are red, cut the wire.
                    case 3:
                        return wireMaterialIndices[1] == 0 || wireMaterialIndices[2] == 0;
                    //If the bomb has at least 3 batteries and the ID number is less than 5000, cut the wire.
                    case 4:
                        return batteryCount >= 3 && digits[0] < 5;
                    //If the third or fifth wire is either yellow or green, cut the wire.
                    case 5:
                        return wireMaterialIndices[2] == 2 || wireMaterialIndices[2] == 3 || wireMaterialIndices[4] == 2 || wireMaterialIndices[4] == 3;
                    //If the third and fifth wires must be cut, cut the wire.
                    case 6:
                        return WireShouldBeCut(2) && WireShouldBeCut(4);
                    //If the defuser failed the second or third gamble attempt, cut the wire.
                    case 8:
                        return gambleAttempt == 2 || gambleAttempt == 3;
                    default:
                        return false;
                }
            //Second wire
            case 1:
                switch(wireCount)
                {
                    //If the second digit of the ID number is even, cut the wire.
                    case 2:
                        return digits[1] % 2 == 0;
                    //If the serial number contains the number 2 or 5, cut the wire.
                    case 3:
                        return serialNumberSpecial;
                    //If the module has at least one blue wire, cut the wire.
                    case 4:
                        for(int c = 0; c < wires.Count; c++)
                        {
                            if(wireMaterialIndices[c] == 1)
                            {
                                return true;
                            }
                        }
                        return false;
                    //If both of the LEDs are green or the top LED is red and the bottom LED is off, cut the wire.
                    case 5:
                        return (LEDMaterialIndices[0] == 2 && LEDMaterialIndices[1] == 2) || (LEDMaterialIndices[0] == 1 && LEDMaterialIndices[1] == 0);
                    //If the bomb has at least 2 batteries but no AA batteries, cut the wire.
                    case 6:
                        return batteryCount >= 2 && aaBatteryCount == 0;
                    //If the module has no yellow wires, cut the wire.
                    case 8:
                        for (int c = 0; c < wires.Count; c++)
                        {
                            if (wireMaterialIndices[c] == 2)
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        return false;
                }
            //Third wire
            case 2:
                switch(wireCount)
                {
                    //If the ID number has more even digits than odd digits, cut the wire.
                    case 3:
                        return evenDigitCount >= 3;
                    //If the bomb has at least one D battery and at least one lit LED, cut the wire.
                    case 4:
                        return dBatteryCount > 0 && (LEDMaterialIndices[0] > 0 || LEDMaterialIndices[1] > 0);
                    //If the last digit of the serial number is even, cut the wire.
                    case 5:
                        return serialLastDigitEven;
                    //If two of the wires are blue, cut the wire.
                    case 6:
                        int blueWireCount = 0;
                        for (int c = 0; c < wires.Count; c++)
                        {
                            if (wireMaterialIndices[c] == 1)
                            {
                                blueWireCount++;
                            }
                        }
                        return blueWireCount == 2;
                    //If the bomb has a green LED and a 3 in either the ID number or the serial number, cut the wire.
                    case 8:
                        return (LEDMaterialIndices[0] == 2 || LEDMaterialIndices[1] == 2) && numberSpecial;
                    default:
                        return false;
                }
            //Fourth wire
            case 3:
                switch(wireCount)
                {
                    //If the serial number has 4 letters, cut the wire.
                    case 4:
                        return serialLetterSpecial;
                    //If the bomb has more unlit indicators than lit indicators, cut the wire.
                    case 5:
                        return unlitIndicatorCount > litIndicatorCount;
                    //If the bomb has a lit indicator FRK or BOB or at least 3 purple wires, cut the wire.
                    case 6:
                        int purpleWireCount = 0;
                        for(int c = 0; c < wires.Count; c++)
                        {
                            if (wireMaterialIndices[c] == 4)
                            {
                                purpleWireCount++;
                            }
                        }
                        return containsSpecificIndicator6Wires || purpleWireCount >= 3;
                    //If the bomb has an unlit indicator SND or NSA or the top LED is off, cut the wire.
                    case 8:
                        return containsSpecificIndicator8Wires || LEDMaterialIndices[0] == 0;
                    default:
                        return false;
                }
            //Fifth wire
            case 4:
                switch(wireCount)
                {
                    //If the bomb has a Serial port but no red LEDs, cut the wire.
                    case 5:
                        return containsSpecificPorts5Wires;
                    //If the bomb has a duplicate port or the ID number is even, cut the wire.
                    case 6:
                        return containsDuplicatePort || digits[3] % 2 == 0;
                    //If the sum of the ID number's digits is less than 17, cut the wire.
                    case 8:
                        return digitSum < 17;
                    default:
                        return false;
                }
            //Sixth wire
            case 5:
                switch(wireCount)
                {
                    //If the bomb has two different LEDs, cut the wire.
                    case 6:
                        return LEDMaterialIndices[0] != LEDMaterialIndices[1];
                    //If at least 4 other wires must be cut, cut the wire.
                    case 8:
                        int wiresToCutSum = 0;
                        //If the special rules apply, it must check 1-6 and 8 rather than 1-5 and 7-8
                        int cutoffIndex = 5;
                        if(specialRules8)
                        {
                            cutoffIndex++;
                        }

                        for(int c = 0; c < cutoffIndex; c++)
                        {
                            if(WireShouldBeCut(c))
                            {
                                wiresToCutSum++;
                            }
                        }
                        for(int c = cutoffIndex + 1; c < wires.Count; c++)
                        {
                            if(WireShouldBeCut(c))
                            {
                                wiresToCutSum++;
                            }
                        }
                        return wiresToCutSum >= 4;
                    default:
                        return false;
                }
            //Seventh wire
            case 6:
                switch(wireCount)
                {
                    //If the bomb has a PS/2, Stereo RCA, or RJ-45 port, cut the wire.
                    case 8:
                        return containsSpecificPorts8Wires;
                    default:
                        return false;
                }
            //Eighth wire
            case 7:
                switch(wireCount)
                {
                    //If this wire is not red and there are no red LEDs, cut the wire.
                    case 8:
                        if(specialRules8)
                        {
                            return wireMaterialIndices[0] != 0 && LEDMaterialIndices[0] != 1 && LEDMaterialIndices[1] != 1;
                        }
                        return wireMaterialIndices[7] != 0 && LEDMaterialIndices[0] != 1 && LEDMaterialIndices[1] != 1;
                    default:
                        return false;
                }
            default:
                return false;
        }
    }

    /// <summary>
    /// Check if all of the wires that need to be cut have been cut
    /// </summary>
    private void CheckSolution()
    {
        //Movement/audio
        submitButton.AddInteractionPunch(.5f);
        bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        //Give a strike if the defuser has not revealed the wires if they didn't gamble to completion
        if(!wiresRevealed && !moduleSolved)
        {
            bombModule.HandleStrike();
            return;
        }

        //If they already gambled to completion, ignore the rest of the method
        if(moduleSolved)
        {
            return;
        }

        //Check if any of the wires that need to be cut have not yet been cut, and give a strike if so
        for(int c = 0; c < wires.Count; c++)
        {
            if(wireRenderers[c].enabled && wiresToCut[c])
            {
                Debug.LogFormat(@"[Risky Wires #{0}] At least wire {1} should have been cut, but was not. Strike occurred.", moduleId, c + 1);
                bombModule.HandleStrike();
                return;
            }
        }

        //If solved correctly, give a pass and flag it as solved
        Debug.LogFormat(@"[Risky Wires #{0}] All wires cut as necessary and submitted. Module passed.", moduleId);
        bombModule.HandlePass();
        moduleSolved = true;
    }
}
