using UnityEngine;
using UnityEngine.UI;

public class ParticleElement : MonoBehaviour, IPoolable
{
    private RectTransform rectTransform;
    private Image image;
    
    public Vector2 pos;
    public Vector2 vel;
    public float life;
    public float rot;
    public float rotSpd;

    public void Initialize(RectTransform rect, Image img)
    {
        rectTransform = rect;
        image = img;
    }

    public void Setup(Vector2 position, Sprite sprite, Vector2 size, Vector2 velocity, float lifetime, float rotation, float rotationSpeed)
    {
        pos = position;
        vel = velocity;
        life = lifetime;
        rot = rotation;
        rotSpd = rotationSpeed;
        
        rectTransform.anchoredPosition = pos;
        rectTransform.sizeDelta = size;
        rectTransform.rotation = Quaternion.Euler(0f, 0f, rot);
        
        image.sprite = sprite;
        image.color = Color.white;
    }

    public void UpdateParticle(float deltaTime, float gravity, float particleLifetime)
    {
        life -= deltaTime;
        vel.y -= gravity * deltaTime;
        pos += vel * deltaTime;
        rot += rotSpd * deltaTime;
        
        rectTransform.anchoredPosition = pos;
        rectTransform.rotation = Quaternion.Euler(0f, 0f, rot);
        
        Color color = image.color;
        color.a = Mathf.Clamp01(life / particleLifetime);
        image.color = color;
    }

    public bool IsExpired()
    {
        return life <= 0f;
    }

    public void OnSpawn()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void OnDespawn()
    {
        life = 0f;
        vel = Vector2.zero;
        rot = 0f;
        rotSpd = 0f;
        
        if (image != null)
        {
            image.color = Color.white;
            image.sprite = null;
        }
    }
}

