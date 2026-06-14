using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum HomeTab
{
  Battle,
  Collection,
  Shop,
  Social,
  Events,
  Debug
}


public class SimpleTabController : MonoBehaviour
{
  [SerializeField] private GameObject shopPanel;
  [SerializeField] private GameObject collectionPanel;
  [SerializeField] private GameObject battlePanel;
  [SerializeField] private GameObject socialPanel;
  [SerializeField] private GameObject eventsPanel;

  private void Start()
  {
    ShowBattle();
  }

  public void ShowBattle()
  {
    battlePanel.SetActive(true);
    collectionPanel.SetActive(false);
    shopPanel.SetActive(false);
    socialPanel.SetActive(false);
    eventsPanel.SetActive(false);
  }

  public void ShowCollection()
  {
    battlePanel.SetActive(false);
    collectionPanel.SetActive(true);
    shopPanel.SetActive(false);
    socialPanel.SetActive(false);
    eventsPanel.SetActive(false);
  }

  public void ShowShop()
  {
    battlePanel.SetActive(false);
    collectionPanel.SetActive(false);
    shopPanel.SetActive(true);
    socialPanel.SetActive(false);
    eventsPanel.SetActive(false);
  }

  public void ShowSocial()
  {
    battlePanel.SetActive(false);
    collectionPanel.SetActive(false);
    shopPanel.SetActive(false);
    socialPanel.SetActive(true);
    eventsPanel.SetActive(false);
  }

  public void ShowEvents()
  {
    battlePanel.SetActive(false);
    collectionPanel.SetActive(false);
    shopPanel.SetActive(false);
    socialPanel.SetActive(false);
    eventsPanel.SetActive(true);
  }
}