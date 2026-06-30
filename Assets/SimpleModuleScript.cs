using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

public class SimpleModuleScript : MonoBehaviour {

	public KMAudio audio;
	public KMBombInfo info;
	public KMBombModule module;
	public KMSelectable[] Doors;
	public KMSelectable[] Buttons;
	static int ModuleIdCounter = 1;
	int ModuleId;

	private int ans = 0;
	private int StageCur = 1;
	private int StageLim = 4;
	private int strikecount = 0;
    private float startTime = 0;

	bool _isSolved = false;
	bool incorrect = false;

	void Awake() {
		ModuleId = ModuleIdCounter++;

		foreach (KMSelectable button in Doors)
		{
			KMSelectable pressedButton = button;
			button.OnInteract += delegate () { door(pressedButton); return false; };
		}
		foreach (KMSelectable button in Buttons)
		{
			KMSelectable pressedButton = button;
			button.OnInteract += delegate () { buttons(pressedButton); return false; };
		}
	}

	void Start ()
	{
		StageCur = 1;
		StageLim = 4;
        startTime = info.GetTime();

        CalcAns();
	}

	void Update()
	{
		int curstrikecount = info.GetStrikes ();
		if (curstrikecount != strikecount) 
		{
			strikecount = curstrikecount;
			CalcAns();
		}
	}

	void CalcAns()
	{
		ans = (startTime % 600 > 300) ? 2 : 5;
		Log(string.Format("Starting answer is {0}", ans));

		if (info.GetStrikes() >= 1)
		{
			ans %= 5;
			Log("Strikes >= 1, Answer %= 5");
		}

		if (info.GetStrikes() >= 3) 
		{
			ans += 3;
			Log("Strikes >= 3, Answer += 3");
		}

		if (info.GetBatteryCount () >= 5) 
		{
			ans *= 3;
			Log("Battery count >= 5, Answer *= 3");
		}

		if (info.GetBatteryHolderCount () >= 3) 
		{
			ans -= 2;
			Log("Battery holder count >= 3, Answer -= 2");
		}

		if (info.GetPortCount () >= 2) 
		{
			ans *= 4;
			Log("Port count >= 2, Answer *= 4");
		}

		if (info.GetPortPlateCount () >= 3) 
		{
			ans *= 2;
			Log("Port plate count >= 3, Answer *= 2");
		}

		Log(string.Format("Final answer is {0}", ans));
	}


	void door(KMSelectable pressedButton)
	{
		int buttonPosition = new int();
		for(int i = 0; i < Doors.Length; i++)
		{
			if (pressedButton == Doors[i])
			{
				buttonPosition = i;
				break;
			}
		}

		audio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, Doors [buttonPosition].transform);
		Doors [buttonPosition].AddInteractionPunch ();
		if (StageCur > 0) 
		{
			if (StageCur < 2) 
			{
				switch (buttonPosition)
				{
				    case 0:
					if(ans > 15)
					{
						incorrect = true;
						Log ("Strike! Ans is more than 15.");
					}
					break;
				    case 1:
					if(ans < 16)
					{
						incorrect = true;
						Log ("Strike! Ans is less than 16");
					}
					break;
				}

				if(incorrect)
				{
					incorrect = false;
					module.HandleStrike ();
					StageCur = 1;
				}
				else
				{
					StageCur = StageCur + 1;
					Log ("stage increase");
				}
			}
		}
		if (StageCur > 2) 
		{
			if (StageCur < 4)
			{
				switch (buttonPosition)
				{
				case 0:
					if(ans > 30)
					{
						incorrect = true;
						Log ("Strike! Ans is more than 30.");
					}
					break;
				case 1:
					if(ans < 31)
					{
						incorrect = true;
						Log ("Strike! Ans is less than 31.");
					}
					break;
				}
				if(incorrect)
				{
					incorrect = false;
					module.HandleStrike ();
					StageCur = 1;
				}
				else
				{
					StageCur = StageCur + 1;
					Log ("stage increase");
				}
			}
		}
	}

	void buttons(KMSelectable pressedButton)
	{
		int buttonPosition = new int();
		for(int i = 0; i < Buttons.Length; i++)
		{
			if (pressedButton == Buttons[i])
			{
				buttonPosition = i;
				break;
			}
		}

		audio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, Buttons [buttonPosition].transform);
		Buttons [buttonPosition].AddInteractionPunch ();
		if (StageCur < 3) 
		{
			if (StageCur > 1) 
			{
				switch (buttonPosition) 
				{
				    case 0:
					if (info.IsTwoFactorPresent () == true) 
					{
						incorrect = true;
						Log ("Strike! There is a two factor code.");
					}
					break;
				case 1:
					if (info.IsTwoFactorPresent () == false) 
					{
						incorrect = true;
						Log ("Strike! There is not a two factor code");
					}
					break;
				}
				if (incorrect) 
				{
					incorrect = false;
					module.HandleStrike ();
					StageCur = 1;
				}
				else
				{
					StageCur = StageCur + 1;
					Log ("stage increase");
				}

			}
		}
		if (StageCur == StageLim) 
		{
			module.HandlePass ();
			Log ("MODULE DEFUSED!");
		}
	}

	void Log(string message)
	{
		Debug.LogFormat("[The Door #{0}] {1}", ModuleId, message);
	}

	//twitch plays
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} left/right/1/2 [Presses the specified door/button] | Presses can be chained with spaces";
	#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		for (int i = 0; i < parameters.Length; i++)
		{
			if (!parameters[i].ToLowerInvariant().EqualsAny("left", "right", "1", "2"))
			{
				yield return "sendtochaterror The specified door/button '" + parameters[i] + "' is invalid!";
				yield break;
			}
		}
		yield return null;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].EqualsIgnoreCase("left"))
				Doors[0].OnInteract();
			else if (parameters[i].EqualsIgnoreCase("right"))
				Doors[1].OnInteract();
			else if (parameters[i].Equals("1"))
				Buttons[0].OnInteract();
			else
				Buttons[1].OnInteract();
			yield return new WaitForSeconds(.1f);
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		if (StageCur == 1)
		{
			Doors[ans <= 15 ? 0 : 1].OnInteract();
			yield return new WaitForSeconds(.1f);
		}
		if (StageCur == 2)
		{
			Buttons[info.IsTwoFactorPresent() ? 1 : 0].OnInteract();
			yield return new WaitForSeconds(.1f);
		}
		if (StageCur == 3)
		{
			Doors[ans <= 30 ? 0 : 1].OnInteract();
			yield return new WaitForSeconds(.1f);
		}
		Buttons[0].OnInteract();
	}
}

