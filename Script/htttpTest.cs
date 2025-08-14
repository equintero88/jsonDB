using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

// Controller principal
public class DeckController : MonoBehaviour
{

    [Header("API (tu JSON falso)")]
    [SerializeField] private string apiBase = "https://my-json-server.typicode.com/equintero88/jsonDB";
    private string UsersEndpoint => $"{apiBase}/users/";    // /users/{id}
    private string CardsEndpoint => $"{apiBase}/cards";     // /cards?value={v}

    [Header("API externa (Rick & Morty)")]
    [SerializeField] private string rickBase = "https://rickandmortyapi.com/api/character/"; // /{id}

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private RawImage avatarImage;      // Avatar del jugador (Rick&Morty)
    [SerializeField] private List<RawImage> cardImageSlots; // 3 RawImage
    [SerializeField] private List<TextMeshProUGUI> cardNameSlots;      // 3 nombres de carta

    [Header("Config")]
    [SerializeField] private int initialUserId = 1;
  

    private int currentUserId;

    private void Start()
    {
        currentUserId = initialUserId;
        StartCoroutine(LoadAll(initialUserId));
    }

    public void NextUser()
{
    
    currentUserId++;
    if (currentUserId > 3) currentUserId = 1; // vuelve al primero
    ClearCardsUI();
    StartCoroutine(LoadAll(currentUserId));
}

    public void ChangeUser(int newUserId)
    {
        ClearCardsUI();
        StartCoroutine(LoadAll(newUserId));
    }

    private IEnumerator LoadAll(int userId)
    {
        // 1) Usuario
        UnityWebRequest userReq = UnityWebRequest.Get(UsersEndpoint + userId);
        yield return userReq.SendWebRequest();

        if (userReq.result != UnityWebRequest.Result.Success || userReq.responseCode != 200)
        {
            Debug.LogError($"User GET error: {userReq.error} (status {userReq.responseCode})");
            yield break;
        }

        User user = JsonUtility.FromJson<User>(userReq.downloadHandler.text);
        if (playerNameText) playerNameText.text = user.username;

        // 2) Avatar desde Rick & Morty 
        int rickId = Mathf.Max(1, user.id); // mapeo 1->1, 2->2, 3->3...
        yield return StartCoroutine(LoadRickAvatar(rickId));

        // 3) Cartas por "value"
        for (int i = 0; i < user.deck.Length && i < cardImageSlots.Count; i++)
        {
            int cardValue = user.deck[i];
            yield return StartCoroutine(LoadCardByValueIntoSlot(cardValue, i));
        }
    }

    private IEnumerator LoadRickAvatar(int characterId)
    {
        UnityWebRequest req = UnityWebRequest.Get(rickBase + characterId);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
        {
            Debug.LogError($"Rick&Morty GET error: {req.error} (status {req.responseCode})");
            yield break;
        }

        RickCharacter rc = JsonUtility.FromJson<RickCharacter>(req.downloadHandler.text);
        if (!string.IsNullOrEmpty(rc.image))
        {
            yield return StartCoroutine(DownloadTextureToRawImage(rc.image, avatarImage));
        }
    }

    private IEnumerator LoadCardByValueIntoSlot(int value, int slotIndex)
    {
        
        string url = $"{CardsEndpoint}?value={value}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
        {
            Debug.LogError($"Card GET error (value={value}): {req.error} (status {req.responseCode})");
            yield break;
        }

       
        string wrapped = "{\"cards\":" + req.downloadHandler.text + "}";
        CardList list = JsonUtility.FromJson<CardList>(wrapped);
        if (list.cards != null && list.cards.Length > 0)
        {
            Card card = list.cards[0];

            // Nombre de la carta
            if (slotIndex < cardNameSlots.Count && cardNameSlots[slotIndex] != null)
                cardNameSlots[slotIndex].text = card.name;

            // Imagen
            if (slotIndex < cardImageSlots.Count)
                yield return StartCoroutine(DownloadTextureToRawImage(card.image, cardImageSlots[slotIndex]));
        }
        else
        {
            Debug.LogWarning($"No se encontró carta con value={value}");
        }
    }

    private IEnumerator DownloadTextureToRawImage(string url, RawImage target)
    {
        if (target == null || string.IsNullOrEmpty(url)) yield break;

        UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(url);
        yield return texReq.SendWebRequest();

        if (texReq.result != UnityWebRequest.Result.Success || texReq.responseCode != 200)
        {
            Debug.LogError($"Img GET error: {url} -> {texReq.error} (status {texReq.responseCode})");
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(texReq);
        target.texture = tex;
    }

    private void ClearCardsUI()
    {
        foreach (var img in cardImageSlots) if (img) img.texture = null;
        foreach (var t in cardNameSlots) if (t) t.text = "";
        if (avatarImage) avatarImage.texture = null;
    }
    

    
}

/* ====== Modelos ====== */
[System.Serializable]
public class User
{
    public int id;
    public string username;
    public bool state;
    public int[] deck; // valores: [2,3,4] etc.
}

[System.Serializable]
public class Card
{
    public int value;   // 
    public string name;
    public string suit;
    public string image; // URL absoluta
}

[System.Serializable]
public class CardList
{
    public Card[] cards;
}

[System.Serializable]
public class RickCharacter
{
    public int id;
    public string name;
    public string species;
    public string image; // URL de la imagen del personaje
}


