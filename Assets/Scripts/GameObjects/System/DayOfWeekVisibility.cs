using System;
using UnityEngine;

class DayOfWeekVisibility : MonoBehaviour
{
    [SerializeField] private DayOfWeek _dayOfWeek;
    private void Awake()
    {
        gameObject.SetActive(_dayOfWeek == GameManager.CurrentDayOfWeek);
    }
}