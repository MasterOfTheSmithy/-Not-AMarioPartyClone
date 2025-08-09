using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileChoiceUI : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab; // Assign a simple arrow GameObject prefab
    [SerializeField] private float arrowYOffset = 1.5f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    private List<GameObject> activeArrows = new List<GameObject>();
    private List<BoardTile> currentChoices = new List<BoardTile>();
    private int selectedIndex = 0;

    private PlayerMover playerMover;

    public void ShowChoicesDirectional(PlayerMover player, List<BoardTile> nextTiles, Vector3 travelDirection)
    {
        ClearArrows();

        playerMover = player;
        currentChoices = nextTiles;

        for (int i = 0; i < nextTiles.Count; i++)
        {
            BoardTile tile = nextTiles[i];
            Vector3 arrowPos = tile.transform.position + Vector3.up * arrowYOffset;

            GameObject arrow = Instantiate(arrowPrefab, arrowPos, Quaternion.identity, transform);
            arrow.name = $"Arrow_{i}";

            Vector3 directionToTile = (tile.transform.position - player.currentTile.transform.position).normalized;
            if (directionToTile != Vector3.zero)
            {
                arrow.transform.rotation = Quaternion.LookRotation(directionToTile, Vector3.up);
            }

            // Ensure collider exists
            Collider col = arrow.GetComponent<Collider>();
            if (col == null)
            {
                col = arrow.AddComponent<BoxCollider>();
            }

            // Ensure Rigidbody is present and set to kinematic (required for EventSystem to detect physics events)
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = arrow.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = true;
            }

            ArrowHoverHandler hoverHandler = arrow.AddComponent<ArrowHoverHandler>();
            hoverHandler.Setup(i, this);

            activeArrows.Add(arrow);
        }

        selectedIndex = 0;
        UpdateArrowHighlight();

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (activeArrows.Count == 0) return;

        // Keyboard navigation
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            selectedIndex = (selectedIndex + 1) % activeArrows.Count;
            UpdateArrowHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selectedIndex = (selectedIndex - 1 + activeArrows.Count) % activeArrows.Count;
            UpdateArrowHighlight();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection();
        }
    }

    private void UpdateArrowHighlight()
    {
        for (int i = 0; i < activeArrows.Count; i++)
        {
            Renderer rend = activeArrows[i].GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = (i == selectedIndex) ? highlightColor : normalColor;
            }
        }
    }

    public void HoverSelect(int index)
    {
        selectedIndex = index;
        UpdateArrowHighlight();
    }

    public void HoverClick()
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < currentChoices.Count)
        {
            playerMover.ReceiveTileChoice(currentChoices[selectedIndex]);
            ClearArrows();
            gameObject.SetActive(false);
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in activeArrows)
        {
            Destroy(arrow);
        }
        activeArrows.Clear();
        currentChoices.Clear();
    }

    public void ShowChoices(PlayerMover player, List<BoardTile> nextTiles)
    {
        Vector3 defaultDirection = Vector3.forward;
        ShowChoicesDirectional(player, nextTiles, defaultDirection);
    }
}

public class ArrowHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private int index;
    private TileChoiceUI ui;

    public void Setup(int index, TileChoiceUI ui)
    {
        this.index = index;
        this.ui = ui;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.HoverSelect(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ui.HoverClick();
    }
}