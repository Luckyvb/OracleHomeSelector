using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using OracleHomeSelector.Properties;


namespace OracleHomeSelector
{
        //A simples STRUCT to store Oracle clinet informations from the registry
        public struct KeySturct
        {
            public string Name;
            public string Home;
            public string Bin;
            public string Nls;
            public string Id;
            public RegistryKey Key;
            public bool IsDefault;
        }

    //Form class
    public partial class Form1 : Form
    {
        public List<KeySturct> Keys;
 
        public Form1(List<KeySturct> keys)
        {
            Keys = keys;
            //Created by Visual Studio.. draw button, visual components to the form etc. All automatic...
            InitializeComponent();
        }
         

        //A list of key_struct... storing more Oracle Client informations in a list structure
        //This is a GLOBAL list: all methods can see it! 
        //This is not very nice, but not needed to give it in parameters between several methods

        private void Form1_Load(object sender, EventArgs e)
        {
            var defFound = false;
            foreach (var key in Keys)
            {
                //...and adding the name to combobox1 items. We can select names from combobox1
                comboBox1.Items.Add(key.Name);
                if (key.IsDefault)
                {
                    defFound = true;
                    comboBox1.Text = key.Name;
                }
            }

            if (!defFound)
            {
                if (comboBox1.Items.Count > 0)
                {
                    //Select the first, wich index is 0
                    comboBox1.SelectedIndex = 0;
                    //Then fire the combobox1_change event.
                    comboBox1_SelectedIndexChanged(sender, e);
                }
            }
        }

        //If comobox1 changes this event will fire
        //that means we chose an Oracle client
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Find the keyitem in KEYS list wich name is equal the name selected in combobox1
            //Nice LAMBDA!!!!
            KeySturct keyitem = Keys.FirstOrDefault(x => x.Name == comboBox1.Text);

            //And gives the Oracle client parameters into textbox1,2,3 ...the home (means a path in the copmputer)
            //Gives back the NLS settings: NLS is national settings ie. AMERICA and char encoding like UNICODE etc like this: HUNGARIAN_HUNGARY.EE8MSWIN1250 ...
            //Textboxes are readonly: we cannot modify parameters, just see them, and set the default Oracle client with this program.
            textBox1.Text = keyitem.Home;
            textBox2.Text = keyitem.Nls;
            textBox3.Text = keyitem.Id;


        }

        //If we select an Oracle client.... Chosing by combobox1, and click SET button...
        private void button1_Click(object sender, EventArgs e)
        {

            //If we want to set nothing...
            if (comboBox1.Text == "")
            {
                //Then do nothing. Cannot set nothing to default Oracle client
                return;
            }

            //Makes a nice hourglass mouse cursor
            Cursor.Current = Cursors.WaitCursor;

            //Key is a registry key type
            RegistryKey key = null;
            //Subkey is ALL_HOMES (see above)
            var subkey = @"Software\Oracle\ALL_HOMES";

            //Try to modify ALL_HOMES
            try
            {
                //Opens ALL_HOMES registry key with WRITE permission : true means write
                key = Registry.LocalMachine.OpenSubKey(subkey, true);

                //Select the keyitem from KEYS list wich nema equal to combobox1 name
                //LAMBDA!!!
                var keyitem = Keys.FirstOrDefault(x => x.Name == comboBox1.Text);
                
                //Sets DEFAULT_HOME value with keyitem name like a registry STRING
                key?.SetValue("DEFAULT_HOME", keyitem.Name, RegistryValueKind.String);


                //To set the default client in registry is not enough
                //We have to set ORACLE HOME, NLS LANG and TNS ADMIN environment variables
                //Some Oracle client uses this environment parametes, some not.
                //After setting environment variables we have to send notification to Windows shell to warn that some environmental parameter changed.

                //And the most important: we have to change the PATH environment parameter. The Oracle clinet uses programs, and dlls in ORAHOME\bin directory
                //All Oracle client have an ORAHOME\bin directory. Programs will use that xxxxx\bin directory, which placed is the FIRST in PATH parameter!!!
                //So we have to modify PATH, but the FIRST Oraclehome\bin must be the choosed (default) Oraclehome's bin path.
                //...and after we set first the selected client's bin path, we delete the other Oracle client's bin paths. Let it be only one (like Highlander)


                //binpath...path of \bin library
                var binpath = keyitem.Home + @"\bin";

                //tnspath is the Oraclehome\network\admin....
                //it is important for ldap.ora, sqlnet.ora, tnsnames.ora files.
                //We will wirte it into TNS_ADMIN environment variable. Not all Oracle client uses this parameter.
                var tnspath = keyitem.Home + @"\NETWORK\ADMIN";

                //Sets ORACLE_HOME, NLS_LANG TNS_ADMIN environment variables from choosed keyitem parameters or tnspath string.
                //This environment variables set to MACHINE variables :EnvironmentVariableTarget.Machine
                Environment.SetEnvironmentVariable("ORACLE_HOME", keyitem.Home, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("NLS_LANG", keyitem.Nls, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("TNS_ADMIN", tnspath, EnvironmentVariableTarget.Machine);


                //Gets the current PATH
                var path = Environment.GetEnvironmentVariable("Path");

                //PAths are separated with semicolon like this: c\; c:\apps; etc. Splits by semicolon into a string-array, and explicite converts into a string-list
                if (path != null)
                {
                    var paths = new List<string>(path.Split(';'));
                
                    //this is a list of strings for new, modified path
                    var newpaths = new List<string> {binpath};

                    //The FIRST value in new PATH will be the choosed Oracle clients's bin path.

                    //This is a little tricky... Iterting will put new PATH together WITHOUT the all oracle bin paths!
                    //Dont forget: the new, selected bin path is the first item in the newpath list
                    //...and this method iterates on all paths, other Oracle client bin paths, and all other used Windows paths....
                    //..and if the item is an existing  Oracle client's bin path, then the items.bin parameter will be not empty...
                    //..and we NOT give it to new path!!!
                    //All other paths we give.

                    //We just give to the new path: 
                    // - the choosed Oracle client  bin path in first position
                    // - and all paths without other Oracle client bin paths

                    //The result is a PATH, starting with choosed Oracle Client's bin path, and all other paths without Oracle client paths.

                    //Iterating on path items
                    foreach (var item in paths)
                    {
                        //Choose keyitem wich bin parameter equals the phisical path
                        var mykeyitem = Keys.FirstOrDefault(x => string.Equals(x.Bin, item, StringComparison.CurrentCultureIgnoreCase));

                        //If path is an Oracle client binpath, then mykeyitem.bin will NOT null, then skip....
                        //..else this path is NOT an Oracle client bin path then concatenate to new path...
                        if (mykeyitem.Bin == null)
                        {
                            newpaths.Add(item);
                        }
                    }

                    //newpath is a string created from a newpaths list. Join is the inverse of Split, makes a string from an array with a semicolon separator. 
                    //I converted the list to an array with .ToArray() 
                    var newpath = string.Join(";", newpaths.ToArray());

                    //Set PATH environmet variable....
                    Environment.SetEnvironmentVariable("Path", newpath, EnvironmentVariableTarget.Machine);
                }

                //And it is important: after setting environmental parameters, we have to send notification to Operating system to warn:something changed!
                Notify.Sendnotify();

                //And we are ready and happy... Default home is set...
                MessageBox.Show(Resources.Message_DefaultHomeIsSet);
            }
              
            catch (Exception ex)
            {
                //..or we are sad, because some error occured. Then error message...
                MessageBox.Show(ex.Message);
            }
            finally
            {
                //Finally we close the opened registry key.
                key?.Close();
                //..and make a dafault (pointer) mouse cursor
                Cursor.Current = Cursors.Default;
            }


        }
    }
}
