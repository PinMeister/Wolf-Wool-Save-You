﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Wolf : MonoBehaviour
{
    public static Wolf player;

    [SerializeField] float playerSpeed = 3;
    [SerializeField] GameObject sweater;
    public Vector2 targetPosition;
    public bool woolHeld;
    public bool escaped;
    public bool howl;
    public bool sheepSpeedBoost;
    public float howlCooldown;
    public int maxHealth = 4;
    public HealthBar healthBar;

    AudioSource[] audioSources;
    Animator mAnimator;
    bool isMoving;
    bool invincible;
    public bool dialogueActive;
    public bool escape;
    float escapeTimer;
    int currentHealth;

    void Start()
    {
        if (player == null)
        {
            player = this;
        }

        woolHeld = false;
        escaped = true;
        sheepSpeedBoost = false;
        howl = false;
        howlCooldown = 0;
        targetPosition = new Vector2Int(0, 0);
        mAnimator = GetComponent<Animator>();
        audioSources = GetComponents<AudioSource>();
        escape = false;
        escapeTimer = 5;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            currentHealth = maxHealth;
        }
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "UpdatedMap")
        {
            if (currentHealth == 0)
            {
                MainMenu main = new MainMenu();
                main.LoadSceneByName("UpdatedMap");
            }

            isMoving = false;

            if (!dialogueActive) // Prevent input during dialogue
            {
                //Basic WASD movement
                if (Input.GetAxisRaw("Horizontal") < 0)
                {
                    transform.Translate(-Vector2.right * playerSpeed * Time.deltaTime);
                    transform.localScale = new Vector3(-2, 2, 2);
                    isMoving = true;
                }
                if (Input.GetAxisRaw("Horizontal") > 0)
                {
                    transform.Translate(Vector2.right * playerSpeed * Time.deltaTime);
                    transform.localScale = new Vector3(2, 2, 2);
                    isMoving = true;
                }
                if (Input.GetAxisRaw("Vertical") < 0)
                {
                    transform.Translate(-Vector2.up * playerSpeed * Time.deltaTime);
                    isMoving = true;
                }
                if (Input.GetAxisRaw("Vertical") > 0)
                {
                    transform.Translate(Vector2.up * playerSpeed * Time.deltaTime);
                    isMoving = true;
                }

                // Left-click to shear sheep, only succeeds when sufficiently close to it
                if (Input.GetMouseButtonDown(0) && !woolHeld)
                {
                    RaycastHit2D mouseHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                    if (mouseHit.collider != null)
                    {
                        // Debug.Log(mouseHit.collider.gameObject.name);
                        if (mouseHit.collider.tag == "Unsheared")
                        {
                            if ((transform.position - mouseHit.transform.position).magnitude < 3)
                            {
                                audioSources[1].Play();
                                mouseHit.collider.tag = "Sheared";
                                woolHeld = true;
                            }
                        }
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit2D mouseHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                    if (mouseHit.collider != null)
                    {
                        if (mouseHit.collider.name == "Dungeon Chest")
                        {
                            if ((transform.position - mouseHit.transform.position).magnitude < 3)
                            {
                                AudioSource[] soundEffects = GameObject.Find("SheepSoundManager").GetComponent<SoundEffectManager>().soundEffectAudioSource;
                                soundEffects[1].Play();
                                audioSources[0].Play();
                                mouseHit.collider.gameObject.GetComponent<DungeonChest>().OpenChest();
                                this.howl = true;
                                this.escape = true;
                                print("VOICE ACQUIRED");
                            }
                        }
                    }
                }

                // Right-click to throw sweater to the indicated position
                if (woolHeld)
                {
                    if (Input.GetMouseButtonDown(1))
                    {
                        audioSources[2].Play();
                        Instantiate(sweater, this.transform.position, Quaternion.identity);
                        targetPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
                        woolHeld = false;
                    }
                }

                // Press space to trigger howl if acquired and howl cooldown reaches 0
                if (howl)
                {
                    GameObject[] unshearedSheep = GameObject.FindGameObjectsWithTag("Unsheared");
                    GameObject[] shearedSheep = GameObject.FindGameObjectsWithTag("Sheared");
                    GameObject[] clothedSheep = GameObject.FindGameObjectsWithTag("Clothed");
                    GameObject[] goldenSheep = GameObject.FindGameObjectsWithTag("Golden");

                    GameObject[] allSheep = unshearedSheep.Concat(shearedSheep).ToArray().Concat(clothedSheep).ToArray().Concat(goldenSheep).ToArray();

                    if (!sheepSpeedBoost)
                    {
                        foreach (GameObject sheep in allSheep) // increase all sheeps' movement speed during escape sequence
                        {
                            sheep.GetComponent<SheepBehavior>().movementSpeed *= 1.5f;
                        }
                        sheepSpeedBoost = true;
                    }
                    if (escape && escapeTimer > 0)
                    {
                        GameObject.Find("UI").transform.GetChild(4).gameObject.SetActive(true);
                        escapeTimer -= Time.deltaTime;
                        CameraPan pan = Camera.main.GetComponent<CameraPan>();
                        pan.player = this.gameObject;
                        pan.GoTo(new Vector3(219, -40, 0), 0);
                        pan.distanceMargin = 0.5f;
                    }

                    if (escapeTimer <= 0)
                    {
                        GameObject.Find("UI").transform.GetChild(4).gameObject.SetActive(false);
                    }

                    if (howlCooldown <= 0)
                    {
                        if (Input.GetKeyDown(KeyCode.Space))
                        {
                            audioSources[0].Play();
                            howlCooldown = 10;
                        }
                    }
                    else
                    {
                        howlCooldown -= Time.deltaTime;

                        if (howlCooldown >= 9)
                        {
                            foreach (GameObject sheep in allSheep)
                            {
                                if ((sheep.transform.position - this.transform.position).magnitude < 10)
                                {
                                    sheep.GetComponent<SheepBehavior>().IsNowFleeing();
                                }
                            }
                        }
                    }
                }
            }
        }

        mAnimator.SetBool("moving", isMoving);
    }



    public void TakeDamage(int damage)
    {
        if (!invincible)
        {
            currentHealth -= damage;
            SoundEffectManager.instance.PlaySheep();
            healthBar.SetHealth(currentHealth);
            invincible = true;
            
            Invoke("SetInvinsibiltyBack", 3.0f);
        } 

    }

    private void SetInvinsibiltyBack()
    {
        invincible = false;
    }

    public Vector2 GetWolfPos() { return transform.position; }

}
