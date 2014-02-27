using LitJson;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using WebSocketSharp;

public class Prefab : MonoBehaviour {

	public Transform Ball;
	public Transform Cube;
	private WebSocket webSocket;
	private Queue queue;
	private int connectionId;

	void Awake() {
		//bool result = Security.PrefetchSocketPolicy ("127.0.0.1", 5000);
		//print ("result = " + result);
		queue = Queue.Synchronized (new Queue());

		webSocket = new WebSocket ("ws://127.0.0.1:5000/");
		webSocket.OnOpen += (object sender, EventArgs e) => {
			print ("OnOpen()");
		};
		webSocket.OnMessage += (object sender, MessageEventArgs e) => {
			print ("OnMessage() " + e.Data);
			queue.Enqueue(e.Data);
		};
		webSocket.OnError += (object sender, ErrorEventArgs e) => {
			print ("OnError() " + e.Message);
		};

		webSocket.Connect ();
	}

	void Update () {
		if (Input.GetButtonUp("Jump")) {
			//Instantiate (Ball, transform.position, transform.rotation);
			//webSocket.Send();
			Dictionary<string, object> json = new Dictionary<string, object>() {
				{ "command", "create" },
				{ "connectionId", connectionId }
			};
			webSocket.Send (JsonMapper.ToJson(json));
		}

		lock (queue.SyncRoot) {
			if (queue.Count != 0) {
				string message = (string)queue.Dequeue();
				Dictionary<string, object> obj = JsonMapper.ToObject<Dictionary<string, object>>(message);
				//print(obj["command"]);
				//print (obj);
				string command = (string)obj["command"];
				if (command == "connect") {
					connectionId = (int)obj["connectionId"];
					print ("connectionId = " + connectionId);
				} else if (command == "create") {
					string type = (string)obj["type"];
					if (type == "ball") {
						Instantiate (Ball, transform.position, transform.rotation);
					} else {
						Instantiate (Cube, transform.position, transform.rotation);
					}
				} else if (command == "draw") {
					DrawData drawData = JsonMapper.ToObject<DrawData>(message);
					print (drawData.data);

					foreach (KeyValuePair<string, string> pair in drawData.data) {
						if (pair.Key == connectionId.ToString()) {
							continue;
						}
						if (pair.Value == "ball") {
							Instantiate (Ball, transform.position, transform.rotation);
						} else {
							Instantiate (Cube, transform.position, transform.rotation);
						}
					} 
				} else {
					print ("other message = " + message);
				}
			}
		}
	}

	void OnDestroy() {
		webSocket.Close();
	}

	public class Person
	{
		// C# 3.0 auto-implemented properties
		public string   Name     { get; set; }
		public int      Age      { get; set; }
		public DateTime Birthday { get; set; }
	}

	class DrawData {
		public string command;
		//public Dictionary<string, string>[] Data;
		public Dictionary<string, string> data;
	}
}
