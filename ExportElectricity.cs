using ICities;
using ColossalFramework;
using ColossalFramework.Plugins;
using UnityEngine;
using System.IO;
using System;

namespace ExportElectricityMod
{
	public static class ExpmHolder
	{
		// Parce que c# ne tolère pas des variable nul dans le nom
		private static Exportable.ExportableManager expm = null;

		public static Exportable.ExportableManager get()
		{
			if (expm == null)
			{
				expm = new Exportable.ExportableManager ();
			}
			return expm;
		}
	}

	public static class Debugger
	{
		// Programme de mise au poit. Écrire ce qui ce passe dans un fichier texte.  C'est ici à cause de la mise au point. Celle ci n'a aucun effet
		// quand cela provient du fichier nommé OnUpdateMoneyAmount.  Peut-être une chose unique que les évènements des gestionnaires ne euvent pas enregistrer ?  Je ne sais pas.
		public static bool enabled = false; // modifier à vrai "true" pour le développement.  Aussi permit sur l'exception automatiquement.
		public static void Write(String s)
		{
			if (!enabled)
			{
				return;
			}

			using (System.IO.FileStream file = new System.IO.FileStream("ExportElectricityModDebug.txt", FileMode.Append)) {
				StreamWriter sw = new StreamWriter(file);
				sw.WriteLine(s);
    	       	sw.Flush();
    	    }
		}
	}

	public class ExportElectricity : IUserMod
	{
		public string Name 
		{
			get { return "Mode d'Export d'Électricité"; }
		}

		public string Description 
		{
			get { return "Gagner de l'argent en utilisant l'électricité non utilisée et (option) autre production."; }
		}

		public void OnSettingsUI(UIHelperBase helper)
		{
			UIHelperBase group = helper.AddGroup("Permet le contrôle des revenus sur le surplus de capacité");
			ExpmHolder.get().AddOptions (group);			
		}
	}

	public class EconomyExtension : EconomyExtensionBase
	{
		private bool updated = false;
		private System.DateTime prevDate;

		public override long OnUpdateMoneyAmount(long internalMoneyAmount)
		{			
            try
            {
                DistrictManager DMinstance = Singleton<DistrictManager>.instance;
                Array8<District> dm_array = DMinstance.m_districts;
                District d;
	            
	            Debugger.Write("\r\n== OnUpdateMoneyAmount ==");

				double sec_per_day = 75600.0; // pour quelques raisons
				double sec_per_week = 7 * sec_per_day;
				double week_proportion = 0.0;
				int export_earnings = 0;
				int earnings_shown = 0;

 				if (dm_array == null)
                {
                	Debugger.Write("early return, dm_array is null");
                    return internalMoneyAmount;
                }

                d = dm_array.m_buffer[0];

				if (!updated) {
					updated = true;
					prevDate = this.managers.threading.simulationTime;
					Debugger.Write("première exécution");
				} else {
					System.DateTime newDate = this.managers.threading.simulationTime;
					System.TimeSpan timeDiff = newDate.Subtract (prevDate);
					week_proportion = (((double) timeDiff.TotalSeconds) / sec_per_week);
					if (week_proportion > 0.0) {
						Debugger.Write("proportion: " + week_proportion.ToString());
						EconomyManager EM = Singleton<EconomyManager>.instance;
						if (EM != null) {
							// ajouter aux revenus							
							export_earnings = (int) ExpmHolder.get().CalculateIncome(d, week_proportion);
							earnings_shown = export_earnings / 100;
							Debugger.Write("Gains Total: " + earnings_shown.ToString());
							EM.AddResource(EconomyManager.Resource.PublicIncome,
								export_earnings,
								ItemClass.Service.None,
								ItemClass.SubService.None,
								ItemClass.Level.None);
						}
					} else {
						Debugger.Write("week_proportion zero");
					}
					prevDate = newDate;
				}	            	
			}
	        catch (Exception ex)
	        {
	        	// cela ne devrait pas arriver mais s'il le fait, recommencer
	        	Debugger.enabled = true;
	        	Debugger.Write("Exception " + ex.Message.ToString());
	        }
			return internalMoneyAmount;
		}
	}
}
