using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] public BallSO ballSO { get; set; }
    public string tileBallname;
    public Vector2 initialPosition;
    public bool dirty;
    public int column { get; set; }
    public int row { get; set; }
    
    private BallSO prevBallSO;

    void Start()
    {
        tileBallname = ballSO.ballName;
        prevBallSO = ballSO;
        initialPosition = this.transform.position;
    }

    void Update()
    {
        if (prevBallSO != ballSO) 
            //&& this.GetComponent<SpriteRenderer>().sprite != Resources.Load<Sprite>("Assets/Sprites/Dot.png"))
        {
            this.GetComponent<SpriteRenderer>().sprite = ballSO.sprite;
            tileBallname = ballSO.ballName;
            prevBallSO = ballSO;
        }
    }

    private void OnMouseDown()
    {
        
        //Debug.Log(initialPosition);
    }

    //private void OnMouseUp()
    //{
    //}
}