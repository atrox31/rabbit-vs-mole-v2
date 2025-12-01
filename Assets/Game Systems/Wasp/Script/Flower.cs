using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    private bool _isFree = true;
    public bool IsFree { get { return _isFree; } }
    private static List<Flower> _instanceList = new List<Flower>(32);
    public static List<Flower> InstnaceList { get { return _instanceList; } }
    [SerializeField] private float _slotHeight = 1f;
    public float GetSlotHeight { get { return _slotHeight; } }

    private void Awake()
    {
        _instanceList.Add(this);
        _slotHeight *= transform.localScale.y;
    }

    public void Occupy()
    {
        _isFree = false;
    }

    public void Free()
    {
        _isFree = true;
    }
}
