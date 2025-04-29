/* 
 * FilterTestVector3.cs
 * Author: Dario Mazzanti (dario.mazzanti@iit.it), 2016
 * 
 * Testing OneEuroFilter utility on a Unity Vector3
 *
 */

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FilteringOneEuro : MonoBehaviour 
{
	public Transform filteredTransform;
	public Transform anchorTransform;

	public Slider frequencySlider;
	public Slider minCutoffSlider;
	public Slider betaSlider;
	public Slider dcutoffSlider;
	Vector3 startingPosition;
	Vector3 offset;

	OneEuroFilter<Vector3> positionFilter;

	public bool filterOn = false;

	public float filterFrequency = 120.0f;
	public float filterMinCutoff = 1.0f;
	public float filterBeta = 0.0f;
	public float filterDcutoff = 1.0f;
	float angle  = 0.0f;

	void Start () 
	{
		positionFilter = new OneEuroFilter<Vector3>(filterFrequency);
	}

	void Update () 
	{
		if(filterOn)
		{
			filterFrequency = frequencySlider.value;
			filterMinCutoff = minCutoffSlider.value;
			filterBeta = betaSlider.value;
			filterDcutoff = dcutoffSlider.value;
			positionFilter.UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
			filteredTransform.position = positionFilter.Filter(anchorTransform.position);
		}
		else
		{
			filteredTransform.position = anchorTransform.position; 
		}
	}
	
	public void SetFilterOn()
	{
		filterOn = !filterOn;
	}
	
}