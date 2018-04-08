using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class PlayerBehaviour : MonoBehaviour {
    // The MQTT connection information
    private MqttClient client;
    public string brokerHostname = "192.168.10.124";
    public int brokerPort = 1883;
    public string userName = "test";
    public string password = "test";

    // Animation states
    private const int idle = 0;
    private const int idleUp = 1;
    private const int idleLeft = 2;
    private const int idleRight = 3;
    private const int down = 4;
    private const int up = 5;
    private const int left = 6;
    private const int right = 7;
    // Animation stuff
    Animator animator;
    private int currentState = idle;

    // Objects to be varied during play
    Rigidbody2D rigidBody2D;
    Tilemap waterTilemap;
    GameObject waterTilemapObject;
    GameObject playerLight;

    Quaternion zeroRotation = new Quaternion(0, 0, 0, 0);
    Vector3 caveEntrance = new Vector3(-80, -11, 0);
    Vector3 caveExit = new Vector3(-68, -20, 0);
    Vector3 doorTrigger = new Vector3(7, 7, 0);
    Vector3 doorTriggerCaveExit = new Vector3(21.75f, -9.75f, 0);

    // Tiles to change
    public TileBase waterTile;
    public TileBase iceTile;

    public TileBase waterLeft;
    public TileBase waterRight;
    public TileBase waterUp;
    public TileBase waterDown;
    public TileBase waterCornerOne;
    public TileBase waterCornerTwo;
    public TileBase waterCornerThree;
    public TileBase waterCornerFour;
    public TileBase iceLeft;
    public TileBase iceRight;
    public TileBase iceUp;
    public TileBase iceDown;
    public TileBase iceCornerOne;
    public TileBase iceCornerTwo;
    public TileBase iceCornerThree;
    public TileBase iceCornerFour;

    public TileBase waterFallOne;
    public TileBase waterFallTwo;
    public TileBase waterFallThree;
    public TileBase iceFallOne;
    public TileBase iceFallTwo;
    public TileBase iceFallThree;
    public TileBase iceFallFour;
    public TileBase iceFallFive;
    public TileBase iceFallSix;
    public TileBase iceFallSeven;
    public TileBase iceFallEight;
    public TileBase iceFallNine;

    public TileBase waterSplashOne;
    public TileBase waterSplashTwo;
    public TileBase waterSplashThree;
    public TileBase iceSplashOne;
    public TileBase iceSplashTwo;
    public TileBase iceSplashThree;
    public TileBase iceSplashFour;
    public TileBase iceSplashFive;
    public TileBase iceSplashSix;
    public TileBase iceSplashSeven;
    public TileBase iceSplashEight;
    public TileBase iceSplashNine;

    // Data variables
    private float currentTemperature;
    private float currentLight;
    private float averageTemperature;
    private float averageLight;

    private dataPacket[] dataCollector = new dataPacket[5];
    private int dataCollectorCounter = 0;

    private bool hardwareOn = true;
    public int speedConstant = 4;

    // Format JSON comes in from Pi
    public class dataPacket
    {
        public float temperature;
        public int light;
    }
    
    // Use this for initialization
    void Start()
    {
        if (brokerHostname != null)
        {
            Connect();
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
            client.Subscribe(new string[] { "dataTopic" }, qosLevels);
        }

        animator = this.GetComponent<Animator>();
        rigidBody2D = this.GetComponent<Rigidbody2D>();
        waterTilemapObject = GameObject.FindWithTag("Water");
        waterTilemap = waterTilemapObject.GetComponent<Tilemap>();
        playerLight = GameObject.FindWithTag("PlayerLight");
        freezeWater();
    }

    //  Use this when application ends
    void OnApplicationQuit()
    {
        client.Disconnect();
    }

    // Update is called once per frame
    void Update () {
        if (dataCollectorCounter < 5 && hardwareOn == true)
        {
            if (Input.GetKey("q"))
            {
                hardwareOn = false;
            }
            return;
        }

        if (dataCollectorCounter == 5 && hardwareOn == true)
        {
            float totalTemp = 0;
            float totalLight = 0;
            // Average calibrated values
            for(int i = 0; i < dataCollectorCounter; i++)
            {
                totalTemp += dataCollector[i].temperature;
                totalLight += dataCollector[i].light;
            }

            averageTemperature = totalTemp / 5;
            averageLight = totalLight / 5;

            Debug.Log("Average temp: " + averageTemperature);
            Debug.Log("Average light: " + averageLight);

            dataCollectorCounter++;
            return;
        }

        // Make light follow player
        Vector3 newLightLocation = new Vector3(this.transform.position.x, this.transform.position.y, -1);
        playerLight.GetComponent<Transform>().SetPositionAndRotation(newLightLocation, zeroRotation);

        if (hardwareOn)
        {
            if (currentTemperature > averageTemperature + 2)
            {
                meltIce();
                // This raises the bridge
                GameObject.FindWithTag("HoleInFloor").GetComponent<TilemapRenderer>().enabled = false;
                GameObject.FindWithTag("HoleInFloor").GetComponent<TilemapCollider2D>().enabled = false;
                GameObject.FindWithTag("HoleInFloorDetails").GetComponent<TilemapRenderer>().enabled = false;
                GameObject.FindWithTag("WaterDetails").GetComponent<TilemapRenderer>().enabled = true;
            }
            else
            {
                freezeWater();
            }

            if (currentLight > averageLight + 200)
            {
                playerLight.GetComponent<Light>().intensity = 5;
            }
            else
            {
                playerLight.GetComponent<Light>().intensity = 0;
            }
        }
        else
        {
            // Turns on light permanently 
            playerLight.GetComponent<Light>().intensity = 5;
        }

        if (Input.GetKey ("w") || Input.GetKey("s") || Input.GetKey("a") || Input.GetKey("d"))
            {
               if (Input.GetKey("w")) //Move Up
                {
                    rigidBody2D.velocity = new Vector2(0, speedConstant);
                    changeAnimationState(up);
                }
                if(Input.GetKey("s")) //Move Down
                {
                    rigidBody2D.velocity = new Vector2(0, -speedConstant);
                    changeAnimationState(down);
            }
                if (Input.GetKey("a")) //Move Left
                {
                    rigidBody2D.velocity = new Vector2(-speedConstant, 0);
                    changeAnimationState(left);
            }
                if (Input.GetKey("d")) //Move Right
                {
                    rigidBody2D.velocity = new Vector2(speedConstant, 0);
                    changeAnimationState(right);
            }
                if (Input.GetKey("w") && Input.GetKey("a")) //Move Up and Left
                {
                    rigidBody2D.velocity = new Vector2(-speedConstant, speedConstant);
                    changeAnimationState(up);
            }
                if (Input.GetKey("w") && Input.GetKey("d")) //Move Up and Right
                {
                    rigidBody2D.velocity = new Vector2(speedConstant, speedConstant);
                    changeAnimationState(up);
            }
                if (Input.GetKey("s") && Input.GetKey("a")) //Move Down and Left
                {
                    rigidBody2D.velocity = new Vector2(-speedConstant, -speedConstant);
                    changeAnimationState(down);
            }
                if (Input.GetKey("s") && Input.GetKey("d")) //Move Down and Right
                {
                    rigidBody2D.velocity = new Vector2(speedConstant, -speedConstant);
                    changeAnimationState(down);
                }
            } else 
			{
                GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                switch(currentState)
                {
                    case up:
                        changeAnimationState(idleUp);
                        break;
                    case down:
                        changeAnimationState(idle);
                        break;
                    case left:
                        changeAnimationState(idleLeft);
                        break;
                    case right:
                        changeAnimationState(idleRight);
                        break;
                }
            }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "DoorTrigger")
        {
            Debug.Log("Scene swap");
            this.GetComponent<Transform>().SetPositionAndRotation(caveEntrance, zeroRotation);
        }

        if(collision.gameObject.tag == "DoorTriggerCaveExit")
        {
            this.GetComponent<Transform>().SetPositionAndRotation(caveExit, zeroRotation);
        }

        if(collision.gameObject.tag == "CaveEntrance")
        {
            this.GetComponent<Transform>().SetPositionAndRotation(doorTrigger, zeroRotation);
        }

        if(collision.gameObject.tag == "CaveExit")
        {
            this.GetComponent<Transform>().SetPositionAndRotation(doorTriggerCaveExit, zeroRotation);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "FireLog")
        {
            if (Input.GetKey("e"))
            {
                GameObject.FindWithTag("Fire").GetComponent<TilemapRenderer>().enabled = true;
                meltIce();
                // This raises the bridge
                GameObject.FindWithTag("HoleInFloor").GetComponent<TilemapRenderer>().enabled = false;
                GameObject.FindWithTag("HoleInFloor").GetComponent<TilemapCollider2D>().enabled = false;
                GameObject.FindWithTag("HoleInFloorDetails").GetComponent<TilemapRenderer>().enabled = false;
                GameObject.FindWithTag("WaterDetails").GetComponent<TilemapRenderer>().enabled = true;
            }
        }
    }

    // Connect to MQTT broker
    private void Connect()
    {
        Debug.Log("Connecting to server: " + brokerHostname);
        client = new MqttClient(brokerHostname);
        string clientId = Guid.NewGuid().ToString();
        try
        {
            client.Connect(clientId);
        }
        catch (Exception e)
        {
            Debug.LogError("ERROR: " + e);
        }
    }

    // Used when message received from sensors
    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string msg = System.Text.Encoding.UTF8.GetString(e.Message);
        Debug.Log("Received message from " + e.Topic + " : " + msg);
        dataPacket jsonObj = JsonUtility.FromJson<dataPacket>(msg);
        currentTemperature = jsonObj.temperature;
        currentLight = jsonObj.light;

        if (dataCollectorCounter < 5)
        {
            dataCollector[dataCollectorCounter] = jsonObj;
            dataCollectorCounter++;
        }
    }

    void changeAnimationState(int state)
    {
        if (currentState == state)
        {
            return; // no new state changes
        }

        switch (state)
        {
            case idle:
                animator.SetInteger("state", idle);
                break;
            case idleUp:
                animator.SetInteger("state", idleUp);
                break;
            case idleLeft:
                animator.SetInteger("state", idleLeft);
                break;
            case idleRight:
                animator.SetInteger("state", idleRight);
                break;
            case down:
                animator.SetInteger("state", down);
                break;
            case up:
                animator.SetInteger("state", up);
                break;
            case left:
                animator.SetInteger("state", left);
                break;
            case right:
                animator.SetInteger("state", right);
                break;
        }

        currentState = state;
    }

    void meltIce()
    {
        waterTilemap.SwapTile(iceTile, waterTile);
        waterTilemap.SwapTile(iceLeft, waterLeft);
        waterTilemap.SwapTile(iceRight, waterRight);
        waterTilemap.SwapTile(iceUp, waterUp);
        waterTilemap.SwapTile(iceDown, waterDown);
        waterTilemap.SwapTile(iceCornerOne, waterCornerOne);
        waterTilemap.SwapTile(iceCornerTwo, waterCornerTwo);
        waterTilemap.SwapTile(iceCornerThree, waterCornerThree);
        waterTilemap.SwapTile(iceCornerFour, waterCornerFour);
        waterTilemap.SwapTile(iceFallOne, waterFallThree);
        waterTilemap.SwapTile(iceFallFour, waterFallTwo);
        waterTilemap.SwapTile(iceFallSeven, waterFallOne);
        waterTilemap.SwapTile(iceSplashOne, waterSplashOne);
        waterTilemap.SwapTile(iceSplashTwo, waterSplashTwo);
        waterTilemap.SwapTile(iceSplashThree, waterSplashThree);
        waterTilemapObject.GetComponent<TilemapCollider2D>().enabled = true;
    }

    void freezeWater()
    {
        waterTilemap.SwapTile(waterTile, iceTile);
        waterTilemap.SwapTile(waterLeft, iceLeft);
        waterTilemap.SwapTile(waterRight, iceRight);
        waterTilemap.SwapTile(waterUp, iceUp);
        waterTilemap.SwapTile(waterDown, iceDown);
        waterTilemap.SwapTile(waterCornerOne, iceCornerOne);
        waterTilemap.SwapTile(waterCornerTwo, iceCornerTwo);
        waterTilemap.SwapTile(waterCornerThree, iceCornerThree);
        waterTilemap.SwapTile(waterCornerFour, iceCornerFour);
        waterTilemap.SwapTile(waterFallThree, iceFallOne);
        waterTilemap.SwapTile(waterFallTwo, iceFallFour);
        waterTilemap.SwapTile(waterFallOne, iceFallSeven);
        waterTilemap.SwapTile(waterSplashOne, iceSplashOne);
        waterTilemap.SwapTile(waterSplashTwo, iceSplashTwo);
        waterTilemap.SwapTile(waterSplashThree, iceSplashThree);
        waterTilemapObject.GetComponent<TilemapCollider2D>().enabled = false;
    }
}
