﻿using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Exportable
{
	public class ExportableManager
	{
		private SortedDictionary<string, Exportable> exportables;		
		public const String CONF = "ExportElectricityModConfig.txt";
		private float multiplier;

		public ExportableManager ()
		{
			exportables = new SortedDictionary<string, Exportable> ();
			multiplier = 1.0f;

			//new ExportableCremation (this);
			new ExportableElementary (this);
			new ExportableHighSchool (this);
			new ExportableUniversity (this);
			new ExportableElectricity (this);
			//new ExportableIncineration (this);
			new ExportableHealth (this);
			new ExportableHeat (this);
			new ExportableJail (this);
			new ExportableSewage (this);
			new ExportableWater (this);

			LoadSettings ();
		}

		public void Log (String msg)
		{
			ExportElectricityMod.Debugger.Write(msg);
		}

		public void AddExportable (Exportable exp)
		{
			exportables.Add (exp.Id, exp);
		}

		public void LoadSettings ()
		{
			Log ("Chargement des préférences");
			try {
				using (System.IO.StreamReader file = 
					new System.IO.StreamReader(CONF, true))
				{					
					String s = file.ReadLine ();
					String [] sections = s.Split(new char[1]{'|'});
					String [] ids;

					ids = sections[0].Split(new char[1]{','});
					if (sections.Length >= 2) {
						multiplier = float.Parse(sections[1]);
					} else {
						multiplier = 1.0f;
					}

					foreach (var id in ids) {
						if (exportables.ContainsKey(id)) {
							exportables[id].SetEnabled(true, false);
						}
					}
				}
			} catch (Exception e) {
				// no file? use defaults
				Log ("Utilisation par défaut : " + e.ToString());
				exportables[Ids.ELECTRICITY].SetEnabled(true);
			}
		}

		public void StoreSettings ()
		{
			Log ("Préférences du magasin");
			try {
				using (System.IO.FileStream file =
					new System.IO.FileStream(CONF, FileMode.Create))
				{
					List<String>enabled_ids = new List<String>();
					StreamWriter sw = new StreamWriter(file);
					foreach (var pair in exportables) {
						if (pair.Value.GetEnabled()) {
							enabled_ids.Add(pair.Key);
						}
					}
					String cs = String.Join(",", enabled_ids.ToArray()) + "|" + multiplier.ToString();
					Log ("Préférence du rangement - activé : " + cs);
					sw.WriteLine(cs);
					sw.Flush();
				}
			} catch (Exception e) {
				Log ("Erreur des paramètres stockés: " + e.ToString());
			}
		}

		public double CalculateIncome (District d, String id, double weekPortion)
		{
			double income = 0.0;

			if (exportables.ContainsKey (id)) {
				Exportable exp = exportables [id];
				if (exp.GetEnabled()) {
					Log ("Calcul des revenus pour " + id);
					income = exp.CalculateIncome (d, weekPortion);
				}
			}

			return income;
		}

		public double CalculateIncome (District d, double weekPortion)
		{
			double total = 0.0;
			Log ("Calcul des revenus");

			foreach (var id in exportables.Keys) {
				total += CalculateIncome (d, id, weekPortion);
			}

			return total * multiplier;
		}

		public void AddOptions (UIHelperBase group)
		{
			LoadSettings ();
			foreach (var id in exportables.Keys) {
				Exportable exp = exportables [id];
				group.AddCheckbox(exp.Description, exp.GetEnabled(), exp.SetEnabled);
			}
			group.AddSlider ("Multiplier", 0.0f, 2.0f, 0.05f, multiplier, MultiplierSliderChanged);
		}

		private void MultiplierSliderChanged(float val)
		{
			multiplier = val;
			StoreSettings();			
		}
	}
}

