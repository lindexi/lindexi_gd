package com.shusheng.dao.impl.test;

import junit.framework.Assert;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.junit4.SpringJUnit4ClassRunner;

import com.shusheng.dao.HomeworkDao;
import com.shusheng.model.Homework;

@RunWith(SpringJUnit4ClassRunner.class)
@ContextConfiguration(locations = "classpath:resources/beans.xml")
public class HomeworkDaoImplTest {

	@Autowired
	private HomeworkDao homeworkDao;
	
	@Test
	public void testGetHomeworks(){
		String username = "aaa";
		int userStatus = 1;
		Assert.assertEquals(0,homeworkDao.getHomeworks(username, userStatus).size());
	}
	
	@Test
	public void testGetSubjects(){
		String username = "aaaa";
		int userStatus = 0;
		homeworkDao.getSubjects(username, userStatus);
	}
	
	@Test
	public void testSave() {
		Homework homework = new Homework();
		homework.setContent("zuoye");
		homework.setEndTime("yyyy-mm-dd");
		homework.setPublishTime("yyyy-mm-dd");
		homework.setPublishUsername("aaa");
		homework.setUserStatus(0);
		homework.setSpecificTime("aaa");
		homeworkDao.save(homework);
//		Assert.assertEquals(2,homeworkDao.getHomeworks("aaa", 0).size());
	}
}
