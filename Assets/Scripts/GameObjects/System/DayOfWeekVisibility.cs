using RabbitVsMole;
using System;
using UnityEngine;

class DayOfWeekVisibility : MonoBehaviour
{
    [SerializeField] private DayOfWeek _dayOfWeek;
    void Awake()
    {
        gameObject.SetActive(_dayOfWeek == GameManager.CurrentDayOfWeek);
    }
}