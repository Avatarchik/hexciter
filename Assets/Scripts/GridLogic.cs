﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GridLogic : MonoBehaviour {

	private ColorSelector colorSelector;

	GridPlace origin;
	List<GridPlace> gridPlaces = new List<GridPlace>();
	List<GridPlace> deadPlaces = new List<GridPlace> ();

	public AnimationCurve ScoreCurve;
	private MoveProgress moves;
	int score;
	int lastMoveScore;
	int level;
	int startScore;
	int targetScore;
	ScoreBar scoreBar;
	public TextMesh scoreTextMesh;
	public GameoverDialog gameoverDialog;
	public bool disabled { get; private set; }

	void Awake () {
		// tell the game to run at 60 fps, maybe put this some where better later
		Application.targetFrameRate = 60;
		scoreBar = GameObject.Find ("GUICamera").GetComponentInChildren<ScoreBar> ();
		moves = GameObject.Find("GUICamera").GetComponentInChildren<MoveProgress>();
		colorSelector = GameObject.Find("GUICamera").GetComponentInChildren<ColorSelector>();
	}

	void Start () {
		level = 1;
		score = 0;
		startScore = 0;
		targetScore = 5000;
		colorSelector.Init ();
		origin = transform.FindChild("Origin").GetComponent<GridPlace>();

		//populate the list of grid places
		GridPlace[] GPs = GetComponentsInChildren<GridPlace> ();
		for (int i = 0; i < GPs.Length; i++) {
			gridPlaces.Add(GPs[i]);
		}

		FadeCam.Instance.FadeIn(() => {StartCoroutine(Utils.SlowSpawnSiblings(origin));});

		
	}

	void UpdateUI(bool lose){
		scoreBar.ReportProgress (score, startScore, targetScore, lose);
		scoreTextMesh.text = score.ToString("N0");
	}

	void NextLevel(){

		//Calculate bonus points
		int scorePer = level * 200;
		score += moves.Remaining() * scorePer;

		//How many (if any) white hexes should be revived
		int revived = moves.Remaining ();

		//Calculate how many white hexes to add
		int leftover = scoreBar.NumBlackHexes ();

		//Setup next level
		moves.ResetMoves(scorePer);
		level++;
		startScore = score;
		targetScore = score + level * 5000;
		
		UpdateUI(leftover != 0);

		//coroutine waits for everything to be at rest
		StartCoroutine (LevelCleanup (leftover, revived));

	}

	bool GridBusy () {
		for (int i = 0; i < gridPlaces.Count; i++) {
			if (gridPlaces[i].busy || gridPlaces[i].reserved) {
				return true;
			}
		}
		return false;
	}

	IEnumerator LevelCleanup (int leftover, int revived) {

		disabled = true;

		while (GridBusy()) {
			yield return new WaitForEndOfFrame();
		}

		bool gameover = false;

		if (revived != 0) {
			for (int i = 0; i < revived && deadPlaces.Count > 0; i++) {
				GridPlace newRevived = deadPlaces[Random.Range(0, deadPlaces.Count)];
				deadPlaces.Remove(newRevived);
				gridPlaces.Add(newRevived);
				newRevived.hexaCube.spawnWhite = false;
				newRevived.hexaCube.Despawn();
				newRevived.hexaCube.Spawn();
			}
		}

		else if (leftover != 0) {
			for (int i = 0; i < leftover && gridPlaces.Count > 0; i++) {
				GridPlace newWhite = gridPlaces[Random.Range(0, gridPlaces.Count)];
				gridPlaces.Remove(newWhite);
				deadPlaces.Add(newWhite);
				newWhite.hexaCube.spawnWhite = true;
				newWhite.hexaCube.Kill();
			}
		}

		if (gridPlaces.Count == 0) {
			gameover = true;
		}
		
		yield return new WaitForSeconds (.7f);

		disabled = false;

		if (gameover) {
			GameOver();
		}
	}


	public void Flood(GridPlace start) {
		lastMoveScore /= 4; // halves your combo bonus
		if (!start.busy){
			Utils.ReserveAll(start);
			StartCoroutine(Utils.FillSiblings(start, colorSelector.Current()));
			DoMove();
		}
	}

	public void Destroy (GridPlace start) {
		int multiplier = 1;
		if (colorSelector.Current () == start.hexaCube.hexColor) multiplier = 2;

		int tally = Utils.TallyScore(start);
		int levelBonus = 100 + (level - 1) * 10;
		int earnedScore = ((int)(lastMoveScore * Mathf.Sqrt(tally/61f) + tally * levelBonus)/10) * 10 * multiplier;
		lastMoveScore = earnedScore;

		score += earnedScore;
		UpdateUI(false);
		StartCoroutine(Utils.KillSiblings(start));

		ObjectPoolManager.Instance.Pop("ScorePopup").GetComponent<ScorePopup>().Show (earnedScore, start.transform.position);
		DoMove ();
	}

	//Anytyime a move is made, this is called
	public void DoMove(){
		//Update count and color chooser
		moves.DoMove();
		colorSelector.NewColor();
	}

	void GameOver () {

		int best = PlayerPrefs.GetInt ("Best_Score");

		if (score > best) {
			PlayerPrefs.SetInt ("Best_Score", score);
			best = score;
		}

		gameoverDialog.Show (score, best);

		#if (UNITY_IPHONE || UNITY_ANDROID)
		Social.ReportScore(score, "30-move", (bool success) => {
			if(success){
				Debug.Log("Posted score!");
			}
			else{
				Debug.Log("Couldn't post score");
			}
		});
		#endif
	}

	void Update () {
		if (score >= targetScore || moves.NoneLeft()){
			NextLevel();
		}
	}
}
