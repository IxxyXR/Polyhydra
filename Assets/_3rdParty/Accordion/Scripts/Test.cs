using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour {

    private UIAccordion accordion;

    private void Awake()
    {
        accordion = GetComponent<UIAccordion>();
        accordion.onSelectElement.AddListener(InfoTest);
    }

    void InfoTest(int i)
    {
        i += 1;
        Debug.Log("You selected Label" + i);
    }
}
