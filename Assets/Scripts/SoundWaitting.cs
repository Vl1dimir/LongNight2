using UnityEngine;
using System.Collections;

public class SoundWaitting : MonoBehaviour {
	public AudioSource _AudioSource;
	public AudioClip Sound;
	public bool loop;
	public float Timer;
	private float TimerDown;
	// Use this for initialization
	void Start () {
		TimerDown = Timer;
	}
	
	// Update is called once per frame
	void Update () {
		if (TimerDown > 0)
			TimerDown -= Time.deltaTime; 
		if (TimerDown < 0)
			TimerDown = 0; 
		if (TimerDown == 0) {
			_AudioSource.PlayOneShot(Sound);
			if(loop)
			{
			TimerDown = Timer;
			}
			else
			{
				gameObject.GetComponent<SoundWaitting>().enabled = false;
			 }
		}
	}
}
