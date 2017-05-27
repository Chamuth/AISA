﻿using AISA.Core;
using AISA.SpeechRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Speech.Recognition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Threading;

namespace AISA
{
    /// <summary>
    /// Interaction logic for MCQ.xaml
    /// </summary>
    public partial class MCQ : Window
    {
        private int currentIndex = -1;
        private string classIndex = "";
        private string Paper = "";

        private SpeechRecognitionEngine _recognizer = new SpeechRecognitionEngine();
        private int[] _answers = new int[Context.currentPaper.questions.Length];

        public MCQ(string _class, string paper_index)
        {
            InitializeComponent();

            //Set the private fields
            classIndex = _class;
            Paper = paper_index;

            //Set the paper name
            PaperName.Content = Context.currentPaper.name;

            //Show the first question
            UpdateQuestion(true);

            //Pause the AISA Handler for concurrent recognitions
            AISAHandler.Pause();

            //Setup the Speech Recognizer
            _recognizer.LoadGrammar(new Grammar(new GrammarBuilder(new Choices(new string[]
            {
                "A","B", "C", "D", "One", "Two", "Three", "Four", "I don't know", "Skip", "No", "Next", "Previous", "Continue"
            }))));

            _recognizer.SpeechRecognized += _recognizer_SpeechRecognized;

            _recognizer.SetInputToDefaultAudioDevice();

            //Start the speech recognition process
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine(e.Result.Text);

            if (e.Result.Text == "I don't know" || e.Result.Text == "Skip" || e.Result.Text == "No" || e.Result.Text == "Next" || e.Result.Text == "Continue")
            {
                //The user does not know the answer for the question
                _answers[currentIndex] = -1;
            }
            else
            {
                if (e.Result.Text == "A" || e.Result.Text == "One")
                {
                    //First answer
                    _answers[currentIndex] = 1;
                }
                else if (e.Result.Text == "B" || e.Result.Text == "Two")
                {
                    //Second answer
                    _answers[currentIndex] = 2;
                }
                else if (e.Result.Text == "C" || e.Result.Text == "Three")
                {
                    //Third answer
                    _answers[currentIndex] = 3;
                }
                else if (e.Result.Text == "D" || e.Result.Text == "Four")
                {
                    //Fourth answer
                    _answers[currentIndex] = 4;
                }
            }

            if (e.Result.Text == "Previous")
            {
                UpdateQuestion(false);
            }
            else
            {
                UpdateQuestion(true);
            }
        }

        private void CompletePaper()
        {
            UploadingPanel.Visibility = Visibility.Visible;

            //Animate the uploading panel to preview the spinner
            var da = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
            da.EasingFunction = new QuinticEase();
            UploadingPanel.BeginAnimation(OpacityProperty, da);

            _recognizer.RecognizeAsyncCancel();

            //Start the uploading functionality
            var threadstart = new ThreadStart(() =>
            {
               var result =  Scholar.Class.PostMCQPaper(Context.previousPaper[0], Context.previousPaper[1], _answers, Properties.Settings.Default.scholarUsername, Properties.Settings.Default.scholarPassword);

                Application.Current.Dispatcher.Invoke(() =>
                {
                   //Do this in the user interface thread
                   var da2 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
                    da2.EasingFunction = new QuinticEase();

                    da2.Completed += (a, b) =>
                    {
                       // Save and close this window
                       AISAHandler.Start();
                        Close();
                    };

                   //Fade the uploading panel out
                   UploadingPanel.BeginAnimation(OpacityProperty, da2);
                });
            });

            var thread = new Thread(threadstart);
            thread.Start();
        }

        private void UpdateQuestion(bool Direction)
        {
            var temp_index = currentIndex;

            //Increase the index of the question
            if (Direction)
            {
                if (Context.currentPaper.questions.Length - 1 != currentIndex)
                {
                    currentIndex++;
                }else
                {
                    // Completed the paper
                    // Continue the uploading process
                    CompletePaper();
                }
            }
            else
            {
                if (currentIndex - 1 >= 0)
                    currentIndex--;
            }

            var q = Context.currentPaper.questions[currentIndex];

            //Set the QuestionIndex
            QuestionIndex.Content = (currentIndex + 1).ToString() + "/" + Context.currentPaper.questions.Length;

            //Set the questions
            QuestionName.Text = q.question;

            //Set the answers
            Answer1.Content = q.answer1;
            Answer2.Content = q.answer2;
            Answer3.Content = q.answer3;
            Answer4.Content = q.answer4;

            if (currentIndex != temp_index)
            {
                //Pronounce my questions
                PronounceQA();
            }

        }

        private void PronounceQA()
        {
            var q = Context.currentPaper.questions[currentIndex];

            //Speak the result
            OpenSpeak.Speak("Question " + (currentIndex + 1).ToString() + ", " + q.question.ToString() + ", " +
                "A: " + q.answer1.ToString() + ", " +
                "B: " + q.answer2.ToString() + ", " +
                "C: " + q.answer3.ToString() + ", " +
                "D: " + q.answer4.ToString()
            );
        }

        private void Previous_MouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateQuestion(false);
        }

        private void Next_MouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateQuestion(true);
        }
    }
}