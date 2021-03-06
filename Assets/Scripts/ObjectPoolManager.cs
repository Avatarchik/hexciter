﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour {
	
	private static ObjectPoolManager _instance;
	public static ObjectPoolManager Instance {
		get {
			if (!_instance) {
				_instance = GameObject.Find("ObjectPoolManager").GetComponent<ObjectPoolManager>();
			}
			return _instance;
		}
		private set {}
	}
	
	Dictionary<string, Stack<GameObject>> objectPool = new Dictionary<string, Stack<GameObject>>();
	Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

	void Awake () {
		foreach (Object o in Resources.LoadAll("PooledObjects")) {
			if (o.GetType() == typeof(GameObject)) {
				prefabs.Add (o.name, o as GameObject);
				objectPool.Add (o.name, new Stack<GameObject>());
			}
		}
	}

	public GameObject Pop (string goName) {
		if (objectPool[goName].Count == 0) {
			GameObject newGameObject = GameObject.Instantiate(prefabs[goName]) as GameObject;
			newGameObject.name = goName;
			objectPool[goName].Push (newGameObject);
		}
		return objectPool[goName].Pop();
	}
	
	public void Push (GameObject go) {
		objectPool[go.name].Push(go);
		go.SetActive(false);
		go.transform.parent = transform;
	}
}
