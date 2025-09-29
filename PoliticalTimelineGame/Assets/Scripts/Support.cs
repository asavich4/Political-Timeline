using UnityEngine;
using UnityEngine.UI;

public class Support : MonoBehaviour
{
    [SerializeField] Slider sliderWorkingClass;
    [SerializeField] Slider sliderMiddleClass;
    [SerializeField] Slider sliderEnforcer;
    [SerializeField] Slider sliderElites;

    [SerializeField] public int workingClass;
    [SerializeField] public int middleClass;
    [SerializeField] public int enforcers;
    [SerializeField] public int elites;

    void Update()
    {
        sliderWorkingClass.value = workingClass;
        sliderMiddleClass.value = middleClass;
        sliderEnforcer.value = enforcers; 
        sliderElites.value = elites;
    }
}
