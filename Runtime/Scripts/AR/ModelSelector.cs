using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelSelector : MonoBehaviour
{
    private static GameObject selectorInstance;

    public int currentSelectedModel = 0;

    void Awake(){
        if(selectorInstance == null){
            selectorInstance = this.gameObject;
            DontDestroyOnLoad(this.gameObject);
        }else if(selectorInstance != this){         
            Destroy(selectorInstance.gameObject);
            selectorInstance = this.gameObject;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void SetCurrentSelectedModel(int i){
        currentSelectedModel = i;
    }
}
