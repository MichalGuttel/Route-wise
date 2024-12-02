import React from 'react';
import { Box, Typography, Container, Button } from '@mui/material';
import { createTheme, ThemeProvider } from '@mui/material/styles';


const theme = createTheme();

const CircleInfo = ({ title, info, date }) => (
  <Box
    sx={{
      width: 200,
      height: 200,
      borderRadius: '50%',
      backgroundColor: 'primary.main',
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'center',
      alignItems: 'center',
      color: 'white',
      margin: '0 20px',
    }}
  >
    <Typography variant="h6">{title}</Typography>
    <Typography variant="body1">{info}</Typography>
    <Typography variant="body2">{date}</Typography>
  </Box>
);

export default function ResultsPage({ pathData, setCurrentPage }) {
  if (!pathData) {
    return <Typography>No data available</Typography>;
  }

  return (
    <ThemeProvider theme={theme}>
      <Container component="main" maxWidth="md">
        <Box
          sx={{
            marginTop: 8,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Typography component="h1" variant="h4" gutterBottom>
            Your Travel Itinerary
          </Typography>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 4,
            }}
          >
            <CircleInfo 
              title="Outbound Flight" 
              info={`${pathData[0].origin} to ${pathData[0].destination}`}
              date={pathData[0].date}
            />
            <CircleInfo 
              title="Hotel" 
              info={pathData[1].name}
              date={`$${pathData[1].price} per night`}
            />
            <CircleInfo 
              title="Return Flight" 
              info={`${pathData[2].origin} to ${pathData[2].destination}`}
              date={pathData[2].date}
            />
          </Box>
          <Button 
            variant="contained" 
            onClick={() => setCurrentPage('home')} 
            sx={{ mt: 4 }}
          >
            Back to Home
          </Button>
        </Box>
      </Container>
    </ThemeProvider>
  );
}





