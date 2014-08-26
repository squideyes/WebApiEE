var indexViewModel;

function RegisterStudent(id, firstName, lastName, age, gender) {
    var self = this;

    self.Id = ko.observable(id);
    self.FirstName = ko.observable(firstName);
    self.LastName = ko.observable(lastName);

    self.FullName = ko.computed(function () {
        return self.FirstName() + " " + self.LastName();
    }, self);

    self.Age = ko.observable(age);
    self.Gender = ko.observable(gender);

    self.genders = [
        "Male",
        "Female"
    ];

    self.addStudent = function () {
        var dataObject = ko.toJSON(this);

        delete dataObject.FullName;

        $.ajax({
            url: '/api/Students',
            type: 'post',
            data: dataObject,
            contentType: 'application/json',
            success: function (data) {
                indexViewModel.studentListViewModel.students.push(
                    new RegisterStudent(
                        data.Id,
                        data.FirstName,
                        data.LastName,
                        data.Age,
                        data.Gender));

                self.FirstName('');
                self.LastName('');
                self.Age('');
            }
        });
    };
}

function StudentList() {
    var self = this;

    self.students = ko.observableArray([]);

    self.getStudents = function () {
        self.students.removeAll();

        $.getJSON('/api/Students', function (data) {
            $.each(data, function (key, value) {
                self.students.push(new RegisterStudent(
                    value.Id,
                    value.FirstName,
                    value.LastName,
                    value.Age,
                    value.Gender));
            });
        });
    };

    self.removeStudent = function (student) {
        $.ajax({
            url: '/api/Students/' + student.Id(),
            type: 'delete',
            contentType: 'application/json',
            success: function () {
                self.students.remove(student);
            }
        });
    };
}

indexViewModel = {
    registerStudentViewModel: new RegisterStudent(),
    studentListViewModel: new StudentList()
};

$(document).ready(function () {
    ko.applyBindings(indexViewModel);

    indexViewModel.studentListViewModel.getStudents();
});