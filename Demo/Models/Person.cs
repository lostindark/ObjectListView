using System;
using System.ComponentModel;
using BrightIdeasSoftware;

namespace ObjectListViewDemo.Models {
    public class Person : INotifyPropertyChanged
    {
        public bool IsActive { get; set; } = true;

        public Person(string name) {
            this.name = name;
        }

        public Person(string name, string occupation, int culinaryRating, DateTime birthDate, double hourlyRate, bool canTellJokes, string photo, string comments) {
            this.name = name;
            this.Occupation = occupation;
            this.culinaryRating = culinaryRating;
            this.birthDate = birthDate;
            this.hourlyRate = hourlyRate;
            this.CanTellJokes = canTellJokes;
            this.Comments = comments;
            this.Photo = photo;
        }

        public Person(Person other) {
            this.name = other.Name;
            this.Occupation = other.Occupation;
            this.culinaryRating = other.CulinaryRating;
            this.birthDate = other.BirthDate;
            this.hourlyRate = other.GetRate();
            this.CanTellJokes = other.CanTellJokes;
            this.Photo = other.Photo;
            this.Comments = other.Comments;
            this.MaritalStatus = other.MaritalStatus;
        }

        [OLVIgnore]
        public string ImageName { get; } = "user";

        // Allows tests for properties.
        [OLVColumn(ImageAspectName = "ImageName")]
        public string Name {
            get { return this.name; }
            set {
                if (this.name == value)
                {
                    return;
                }

                this.name = value;
                this.OnPropertyChanged(nameof(Name));
            }
        }
        private string name;

        [OLVColumn(ImageAspectName = "ImageName")]
        public string Occupation {
            get { return this.occupation; }
            set {
                if (this.occupation == value)
                {
                    return;
                }

                this.occupation = value;
                this.OnPropertyChanged(nameof(Occupation));
            }
        }
        private string occupation;

        public int CulinaryRating {
            get { return this.culinaryRating; }
            set { this.culinaryRating = value; }
        }
        private int culinaryRating;

        public DateTime BirthDate {
            get { return this.birthDate; }
            set { this.birthDate = value; }
        }
        private DateTime birthDate;

        public int YearOfBirth {
            get { return this.BirthDate.Year; }
            set { this.BirthDate = new DateTime(value, this.birthDate.Month, this.birthDate.Day); }
        }

        // Allow tests for methods
        public double GetRate() {
            return this.hourlyRate;
        }
        private double hourlyRate;

        public void SetRate(double value) {
            this.hourlyRate = value;
        }

        public string Photo { get; set; }
        public string Comments { get; set; }
        public int serialNumber { get; set; }
        public bool? CanTellJokes { get; set; }

        // Allow tests for enums
        public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Single;

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}