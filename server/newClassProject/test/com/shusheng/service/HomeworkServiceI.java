package com.shusheng.service;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

import com.shusheng.model.Homework;
import com.shusheng.model.Subject;

public class HomeworkServiceI {

	public List<Homework> getHomeworks() {
		List<Homework> list = new ArrayList<Homework>();
		for (int i = 0; i < 3; i++) {
			Homework homework = new Homework();
			homework.setPublishUsername("杨树生"+i);
			homework.setSubject("高等数学"+i);
			homework.setPublishTime(new SimpleDateFormat("yyyy-MM-dd").format(new Date()));
			homework.setContent("yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy"+i);
			homework.setEndTime(new SimpleDateFormat("yyyy-MM-dd").format(new Date()));
			homework.setSpecificTime("第一二节课");
			list.add(homework);
		}
		return list;
	}

	public boolean addHomework(String subject, String content, String endTime, String specificTime) {
		return true;
//		return false;
		
	}

	public List<Subject> getSubjects(String userId) {
		List<Subject> subjects = new ArrayList<Subject>();
		for (int i = 0; i < 3; i++) {
			Subject subject = new Subject();
			subject.setName("高等数学"+i);
			subjects.add(subject);
		}
		return subjects;
	}

}
