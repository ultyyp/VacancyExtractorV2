using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

namespace VacancyExtractorV2
{ 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        public class Vacancy
        {
            public string VacancyName { get; set; }
            public string VacancyURL { get; set; }
            public string PublishingDate { get; set; }

        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //int count = 0;

            var url = "https://proglib.io/vacancies/all?workType=all&workPlace=all&experience=&salaryFrom=&page=1";
            using var client = new HttpClient();
            var body = await client.GetStringAsync(url);

            string pattern = @"<div class=""feed-pagination flex align-center"" data-current=""(.+?)"" data-total=""(.+?)"">";
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(body);

            int totalpages = int.Parse(matches[0].Groups[2].Value);

            StatusLabel.Content = "Extracting...";

            ConcurrentBag<Vacancy> cb = await extractVacancies(totalpages);

            foreach( Vacancy vacancy in cb )
            {
                VacancyGrid.Items.Add(vacancy); 
            }

            StatusLabel.Content = "Done!";
            //AmmountLabel.Content = "Vacancies: " + VacancyGrid.Items.Count.ToString();
            AmmountLabel.Content = "Vacancies: " + cb.Count().ToString() + "/" + cb.Distinct().Count().ToString() + "/" +VacancyGrid.Items.Count.ToString();



        }

        public async Task<ConcurrentBag<Vacancy>> extractVacancies(int totalPages)
        {
            ConcurrentBag<Vacancy> concBag = new ConcurrentBag<Vacancy>();
            int[] ints = new int[totalPages];
            for(int i = 0; i< ints.Length; i++)
            {
                ints[i] = i;
            };
            
            var options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 20;
            var token = options.CancellationToken;
            


                await Parallel.ForEachAsync(ints, options, async (index, token) =>
                {
                    

                    //StatusLabel.Content = "Extracting... " + "(" + i.ToString() + "/" + totalPages.ToString() + ")";

                    var loopurl = "https://proglib.io/vacancies/all?workType=all&workPlace=all&experience=&salaryFrom=&page=" + index.ToString();
                    using var loopclient = new HttpClient();
                    var loopbody = await loopclient.GetStringAsync(loopurl);

                    string pattern1 = @"itemprop=""title\"">(.+?)<\/h2>";
                    Regex regex1 = new Regex(pattern1);
                    MatchCollection matches1 = regex1.Matches(loopbody);

                    string pattern2 = @"<a href=""(.+?)"" class=""no-link"">";
                    Regex regex2 = new Regex(pattern2);
                    MatchCollection matches2 = regex2.Matches(loopbody);

                    string pattern3 = "                class=\"publish-info\"\n                title=\"(.+?)\">\n                (.+?)";
                    Regex regex3 = new Regex(pattern3, RegexOptions.Multiline);
                    MatchCollection matches3 = regex3.Matches(loopbody);


                    for (int j = 0; j < matches1.Count; j++)
                    {
                        Vacancy vacancy = new Vacancy();

                        vacancy.VacancyName = matches1[j].Groups[1].Value;

                        vacancy.VacancyURL = "https://proglib.io" + matches2[j].Groups[1].Value;

                        vacancy.PublishingDate = matches3[j].Groups[1].Value;

                        concBag.Add(vacancy);


                    }

                        //count = VacancyGrid.Items.Count;
                        //AmmountLabel.Content = "Vacancies: " + count.ToString();

                        //if (i == totalpages)
                        //{
                        //    StatusLabel.Content = "Done!";
                        //}

                });

            return concBag;

        }

        private void VacancyGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vacancy = (Vacancy)VacancyGrid.SelectedItem;
            Clipboard.SetText(vacancy.VacancyURL);
            MessageBox.Show("Vacancy Link Copied!");
        }
    }


}




