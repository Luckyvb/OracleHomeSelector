using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using OracleHomeSelector.Properties;

namespace OracleHomeSelector
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var I = WindowsIdentity.GetCurrent();

            //amiadmin is true if the user  an administrator
            var amiadmin = AmIAdmin(I);

            //If the user a simple user then....
            if (!amiadmin)
            {
                //Error message, and game over: exit from application...
                MessageBox.Show(Resources.Message_YouAreNotAdministrator);
                //..here..
                Application.Exit();
            }

            var keys = LoadHomes();
            if (keys.Count == 0)
            {
                MessageBox.Show(Resources.Message_NoOracleHomes);
                //..here..
                Application.Exit();
            }
            else
                Application.Run(new Form1(keys));
        }

        static List<KeySturct> LoadHomes()
        {
            var keys = new List<KeySturct>();
            // Get current user's identity (me)
            //If  the user is an administrator, then let start the program

            //Key is a registrykey type 
            RegistryKey key = null;
            //subkey where the Oracle puts its informations.
            //@ means to ignore the \ sign in string.

            var subkeys = new [] { @"SOFTWARE\WOW6432Node\ORACLE", @"SOFTWARE\ORACLE" };

            try
            {
                foreach (var subkey in subkeys)
                {
                    //Try to open the registry key: HKLM\SOFTWARE\ORACLE with READ access: the false means the read permission
                    key = Registry.LocalMachine.OpenSubKey(subkey, false);

                    //If key not exists, or something wrong: ie not have permission to open this key... But here I must be an administrator.
                    if (key == null)
                    {
                        continue;
                        //There isn'Oracle in registry...opens messagebox with OK button
                        //MessageBox.Show(Resources.Message_NoOracleHomes, Resources.Message_Error,
                        //MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                        //and returns to main form.
                        //return keys;
                    }

                    //If REgistry key open was successfull, then iterate on Subkey_names

                    foreach (var item in key.GetSubKeyNames())
                    {
                        // Oracle client informations are in subkeys like folders. And folder names start with HOME in older Oracle clients, and KEY in newer clients.
                        //ie. HOME1 with Client version 9
                        //and Key-Orahome1... with 11 or 12 version
                        //I put keynames into lower to avoid case sensitive problems in string comparisons
                        if (item.ToLower().StartsWith("home") || item.ToLower().StartsWith("key"))
                        {
                            //Item name \\ means \ 
                            var mykey = subkey + "\\" + item;
                            //Opens subkey to read client informations. Write access not necessary (false)
                            var orakey = Registry.LocalMachine.OpenSubKey(mykey, false);

                            //keyitem is a struct type
                            if (orakey != null)
                            {
                                var keyitem = new KeySturct
                                {
                                    Key = orakey,
                                    Name = (string)orakey.GetValue("ORACLE_HOME_NAME", null),
                                    Home = (string)orakey.GetValue("ORACLE_HOME", null)
                                };

                                //read parameters from registry, and copy into keyitem, what is a type of keystruct.
                                keyitem.Bin = keyitem.Home + @"\bin";
                                keyitem.Nls = (string)orakey.GetValue("NLS_LANG", null);
                                keyitem.Id = (string)orakey.GetValue("ID", null);

                                //Adding keyitem to a keystruct LIST
                                keys.Add(keyitem);

                                //...and adding the name to combobox1 items. We can select names from combobox1
                                //comboBox1.Items.Add(keyitem.Name);
                            }
                        }

                    }
                    //If there isn't ALL_HOMES, the select the first in the combobox1

                    //There is a bug in program: if there isn't ALL_HOMES the the program gives back the first client in the list, and not the default client
                    //In newer Oracle clients has no ALL_HOMES in registry.
                    //But this is not a very big problem: if we use this program, set the Oracle client as we wish, and this program will WRITE ALL_HOMES key like in old Oracle versions.
                    //And next time this program  will work well. So this is a very first running problem with newer Oracle clients.
                }

                //Older oracle versions has HKLM\SOFTWARE\ORACLE\ALL_HOMES key. Newer ones has not
                var allhomes = subkeys[0] + "\\ALL_HOMES";

                //Trying to read the deafultkey from ALL_HOMES
                //Makes Defaultkey what is a registrykey
                var defaultkey = Registry.LocalMachine.OpenSubKey(allhomes, false);

                //If ALL_HOMES key EXISTS then
                if (defaultkey != null)
                {
                    //gets the defaulthome name from defaultkey
                    var defaulthome = (string)defaultkey.GetValue("DEFAULT_HOME", null);

                    //Select the keyitem from KEYS list, which name is equal the default home name
                    //Nice, nice, LAMBDA expression!!!!
                    //Not easy... try to understand : https://msdn.microsoft.com/en-us/library/bb397687.aspx

                    var i = 0;
                    for (; i < keys.Count; i++)
                    {
                        if (keys[i].Name == defaulthome)
                        {
                            break;
                        }
                    }
                    if (i < keys.Count)
                    {
                        var x = keys[i];
                        x.IsDefault = true;
                        keys[i] = x;
                    }

                    //Set the combobox1, the home-name selector default value the default home name
                    //comboBox1.Text = keyitem.Name;
                }
            }
            //If something is wrong...
            catch (Exception ex)
            {
                //Messagebox with OK button
                MessageBox.Show(ex.Message, Resources.Message_Error, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
            // and we close the  Registry key after this session
            finally
            {
                key?.Close();
            }

            return keys;
        }

        //This is a very simple method to check: have me administrator role or not? To write registry I have to be in administrators group
        //The program runs in elevated administrator role (set in app.manifest)
        //If the user is not an administrator, then gives an error message.
        //input: a windows identity
        public static bool AmIAdmin(WindowsIdentity user)
        {
            //return default false: user is not administrator
            bool ret;
            try
            {
                //Create a windowsprincipal from user's windows identity
                var principal = new WindowsPrincipal(user);
                //returns true, if user is a member of administrators group
                ret = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            // If something goes to wrong, returns false, that means user is NOT administrator
            catch
            {
                ret = false;
            }

            //return
            return ret;
        }

    }
}
