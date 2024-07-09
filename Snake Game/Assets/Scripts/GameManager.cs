using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Skauron
{
    public class GameManager : MonoBehaviour
    {
        public int maxHeight = 15;
        public int maxWidth = 17;

        public Color color1;
        public Color color2;
        public Color appleColor = Color.red;
        public Color playerColor = Color.black;

        public Transform cameraHolder;

        GameObject playerObj;
        GameObject appleObj;
        GameObject tailParent;
        Node playerNode;
        Node prevPlayerNode;
        Node appleNode;
        Sprite playerSprite;

        GameObject mapObject;
        SpriteRenderer mapRenderer;

        Node[,] grid;
        List<Node> availableNodes = new List<Node>();
        List<SpecialNode> tail = new List<SpecialNode>();

        bool up, left, right, down;
        public float moveRate = 0.5f;
        float timer;
        public bool isGameOver;
        public bool isFirstInput;

        int currentScore;
        int highScore;

        Direction targetDirection;
        Direction currentDirection;
        public enum Direction
        {
            up, down, left, right
        }

        public TextMeshProUGUI currentScoreText;
        public TextMeshProUGUI highScoreText;

        public UnityEvent onStart;
        public UnityEvent onGameOver;
        public UnityEvent FirstInput;
        public UnityEvent onScore;

        #region  Init
        private void Start()
        {
            onStart.Invoke();
        }

        public void StartNewGame()
        {
            ClearReferences();
            CreateMap();
            PlacePlayer();
            PlaceCamera();
            CreateApple();
            isGameOver = false;
            targetDirection = Direction.right;
            currentScore = 0;
            UpdateScore();
        }

        public void ClearReferences()
        {
            if (mapObject != null)
                Destroy(mapObject);
            if (playerObj != null)
                Destroy(playerObj);
            if (appleObj != null)
                Destroy(appleObj);

            foreach (var item in tail)
            {
                if (item.obj != null)
                    Destroy(item.obj);
            }
            tail.Clear();
            availableNodes.Clear();
            grid = null;
        }

        void CreateMap()
        {
            mapObject = new GameObject("Map");
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();

            grid = new Node[maxWidth, maxHeight];

            Texture2D txt = new Texture2D(maxWidth, maxHeight);
            for (int i = 0; i < maxWidth; i++)
            {
                for (int j = 0; j < maxHeight; j++)
                {
                    Vector3 tp = Vector3.zero;
                    tp.x = i;
                    tp.y = j;

                    Node n = new Node()
                    {
                        x = i,
                        y = j,
                        worldPosition = tp
                    };

                    grid[i, j] = n;

                    availableNodes.Add(n);

                    #region Visual
                    if (i % 2 != 0)
                    {
                        if (j % 2 != 0)
                        {
                            txt.SetPixel(i, j, color1);
                        }
                        else
                        {
                            txt.SetPixel(i, j, color2);
                        }
                    }
                    else
                    {
                        if (j % 2 != 0)
                        {
                            txt.SetPixel(i, j, color2);
                        }
                        else
                        {
                            txt.SetPixel(i, j, color1);
                        }
                    }
                    #endregion


                }
            }
            txt.filterMode = FilterMode.Point;

            txt.Apply();
            Rect rect = new Rect(0, 0, maxWidth, maxHeight);
            Sprite sprite = Sprite.Create(txt, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;
        }

        void PlacePlayer()
        {
            playerObj = new GameObject("Player");
            SpriteRenderer playerRender = playerObj.AddComponent<SpriteRenderer>();
            playerSprite = CreateSprite(playerColor);
            playerRender.sprite = playerSprite;
            playerRender.sortingOrder = 1;
            playerNode = GetNode(3, 3);
            PlacePlayerObject(playerObj, playerNode.worldPosition);
            playerObj.transform.localScale = Vector3.one * 1.2f;

            tailParent = new GameObject("tailParent");
        }

        void PlaceCamera()
        {
            Node n = GetNode(maxWidth / 2, maxHeight / 2);
            Vector3 p = n.worldPosition;
            p += Vector3.one * .5f;
            cameraHolder.position = p;
        }

        void CreateApple()
        {
            appleObj = new GameObject("Apple");
            SpriteRenderer appleRenderer = appleObj.AddComponent<SpriteRenderer>();
            appleRenderer.sprite = CreateSprite(appleColor);
            appleRenderer.sortingOrder = 1;
            RandomlyPlaceApple();
        }

        #endregion

        #region  Update
        private void Update()
        {
            if (isGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    onStart.Invoke();
                }
                return;
            }


            GetInput();


            if (isFirstInput)
            {
                SetPlayerDirection();
                timer += Time.deltaTime;
                if (timer > moveRate)
                {
                    timer = 0;
                    currentDirection = targetDirection;
                    MovePlayer();
                }
            }
            else
            {
                if (up || down || left || right)
                {
                    isFirstInput = true;
                    FirstInput.Invoke();
                }
            }
        }
        void GetInput()
        {
            up = Input.GetButtonDown("Up");
            down = Input.GetButtonDown("Down");
            right = Input.GetButtonDown("Right");
            left = Input.GetButtonDown("Left");
        }

        void SetPlayerDirection()
        {
            if (up)
            {
                SetDirection(Direction.up);
            }
            else if (down)
            {
                SetDirection(Direction.down);
            }
            else if (left)
            {
                SetDirection(Direction.left);
            }
            else if (right)
            {
                SetDirection(Direction.right);
            }
        }

        void SetDirection(Direction d)
        {
            if (!isOpposite(d))
            {
                targetDirection = d;
            }
        }

        void MovePlayer()
        {
            int x = 0;
            int y = 0;
            switch (currentDirection)
            {
                case Direction.up:
                    y = 1;
                    break;
                case Direction.down:
                    y = -1;
                    break;
                case Direction.right:
                    x = 1;
                    break;
                case Direction.left:
                    x = -1;
                    break;
            }

            Node targetNode = GetNode(playerNode.x + x, playerNode.y + y);
            if (targetNode == null)
            {
                onGameOver.Invoke();
            }
            else
            {
                if (isTailNode(targetNode))
                {
                    onGameOver.Invoke();
                }
                else
                {
                    bool isScore = false;
                    if (targetNode == appleNode)
                    {
                        isScore = true;
                    }

                    Node previousNode = playerNode;
                    availableNodes.Add(previousNode);

                    if (isScore)
                    {
                        tail.Add(CreateTailNode(previousNode.x, previousNode.y));
                        availableNodes.Remove(previousNode);
                    }

                    MoveTail();

                    PlacePlayerObject(playerObj, targetNode.worldPosition);
                    playerNode = targetNode;
                    availableNodes.Remove(playerNode);

                    if (isScore)
                    {
                        currentScore++;
                        moveRate -= 0.001f;
                        if (currentScore > highScore)
                            highScore = currentScore;

                        onScore.Invoke();
                        
                        if (availableNodes.Count > 0)
                        {
                            RandomlyPlaceApple();
                        }
                        else
                        {
                            //You Won
                        }
                    }
                }
            }
        }

        void MoveTail()
        {
            Node prevNode = null;

            for (int i = 0; i < tail.Count; i++)
            {
                SpecialNode p = tail[i];
                availableNodes.Add(p.node);

                if (i == 0)
                {
                    prevNode = p.node;
                    p.node = playerNode;
                }
                else
                {
                    Node prev = p.node;
                    p.node = prevNode;
                    prevNode = prev;
                }

                availableNodes.Remove(p.node);
                PlacePlayerObject(p.obj, p.node.worldPosition);
            }
        }
        #endregion

        #region  Utilities

        public void UpdateScore()
        {
            currentScoreText.text = currentScore.ToString();
            highScoreText.text = highScore.ToString();
        }

        public void GameOver()
        {
            isGameOver = true;
            isFirstInput = false;
        }

        bool isOpposite(Direction d)
        {
            switch (d)
            {
                default:
                case Direction.up:
                    if (currentDirection == Direction.down)
                        return true;
                    else
                        return false;
                case Direction.down:
                    if (currentDirection == Direction.up)
                        return true;
                    else
                        return false;
                case Direction.right:
                    if (currentDirection == Direction.left)
                        return true;
                    else
                        return false;
                case Direction.left:
                    if (currentDirection == Direction.right)
                        return true;
                    else
                        return false;
            }
        }

        bool isTailNode(Node n)
        {
            for (int i = 0; i < tail.Count; i++)
            {
                if (tail[i].node == n)
                {
                    return true;
                }
            }
            return false;
        }

        void PlacePlayerObject(GameObject obj, Vector3 pos)
        {
            pos += Vector3.one * .5f;
            obj.transform.position = pos;
        }

        void RandomlyPlaceApple()
        {
            int ran = Random.Range(0, availableNodes.Count);
            Node n = availableNodes[ran];
            PlacePlayerObject(appleObj, n.worldPosition);
            appleNode = n;
        }

        Node GetNode(int x, int y)
        {
            if (x < 0 || x > maxWidth - 1 || y < 0 || y > maxHeight - 1)
                return null;

            return grid[x, y];
        }

        SpecialNode CreateTailNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x, y);
            s.obj = new GameObject();
            s.obj.transform.parent = tailParent.transform;
            s.obj.transform.position = s.node.worldPosition;
            s.obj.transform.localScale = Vector3.one * .95f;
            SpriteRenderer r = s.obj.AddComponent<SpriteRenderer>();
            r.sprite = playerSprite;
            r.sortingOrder = 1;

            return s;
        }

        Sprite CreateSprite(Color targetColor)
        {
            Texture2D txt = new Texture2D(1, 1);
            txt.SetPixel(0, 0, targetColor);
            txt.Apply();
            txt.filterMode = FilterMode.Point;
            Rect rect = new Rect(0, 0, 1, 1);
            return Sprite.Create(txt, rect, Vector2.one * .5f, 1, 0, SpriteMeshType.FullRect);
        }
        #endregion
    }
}