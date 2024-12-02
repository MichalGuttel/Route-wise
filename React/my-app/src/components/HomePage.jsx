import React, { useState } from 'react';
// import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
// import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import CssBaseline from '@mui/material/CssBaseline';
import TextField from '@mui/material/TextField';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import ResultsPage from './ResultsPage';

import Box from '@mui/material/Box';
// import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import Typography from '@mui/material/Typography';
import Container from '@mui/material/Container';
import { createTheme, ThemeProvider } from '@mui/material/styles';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import Select from '@mui/material/Select';
import DateRangePicker from './DateRangePicker';
import { DateTimePicker } from '@mui/x-date-pickers';
import axios from 'axios';
import { Autocomplete, Checkbox, FormControlLabel } from '@mui/material';

import iataCodes from '../IATA_CODE.json';
import dayjs from 'dayjs';
import { ar } from 'date-fns/locale';

// // function Copyright(props) {
// //     return (
// //         <Typography variant="body2" color="text.secondary" align="center" {...props}>
// //             {'Copyright © '}
// //             <Link color="inherit" href="https://mui.com/">
// //                 Your Website
// //             </Link>{' '}
// //             {new Date().getFullYear()}
// //             {'.'}
// //         </Typography>
// //     );
// // }

// // TODO remoconst defaultTheme = createTheme();
const defaultTheme = createTheme();

export default function HomePage({setCurrentPage, setPathData}) {

    const [countries, setCountries] = useState(['SFO', 'JFK']);
    const [selectedOriginCountry, setSelectedOriginCountry] = useState('');
    const [selectedDestinationCountry, setSelectedDestinationCountry] = useState('');
    const [startDate, setStartDate] = useState(null);
    const [endDate, setEndDate] = useState(null);
    const [error, setError] = useState();
    const [countriesOriginOptions, setCountriesOriginOptions] = useState(iataCodes.map((option) => option.city));
    const [countriesDestinationOptions, setCountriesDestinationOptions] = useState(iataCodes.map((option) => option.city));
    const[maxPrice,setMaxPrice]=useState('');
    
    const[numAdult,setnumAdult]=useState('');
    const getTodayDate = () => {
        return dayjs();
    }

    const selectedCountry = (type, value) => {
        var obj = iataCodes.find(i => i.city == value);
        if (type == "origin")
            setSelectedOriginCountry(obj);
        else if (type == "destination")
            setSelectedDestinationCountry(obj);
    }

    const handleCheckBox = async (e) => {
        if (e.target.checked == true)
            await getPopulationCountries();
        else setCountriesDestinationOptions(iataCodes.map((option) => option.city))

    }
    const getPopulationCountries = async () => {
        try {
            const response = await axios.get('https://localhost:7125/api/Travel/search-destinations', {
                params: {
                    origin: selectedOriginCountry.code,
                }
            })

            // var resArr=response.data;
            var countriesArr = [];
            // console.log(resArr);

            // resArr.forEach(c => {
            //     var obj=iataCodes.find(i=>i.code==c);
            //     console.log(obj);
            //     countriesArr.push(obj.city);
            // });
            response.data.map(r => {
            
                countriesArr.push(iataCodes.find(i => i.code === r))
            })
            // setCountriesDestinationOptions(countriesArr);

            console.log('arr', countriesArr);

            var arr=[];
            for (let index = 0; index < countriesArr.length; index++) {
                arr.push(countriesArr[index].city)
            }
            console.log('two-arr',arr);
            setCountriesDestinationOptions(arr);
        }
        catch (err) {
            console.error(err);
        }

    }

    const handleSubmit =async (event) => {
        if (startDate == null || endDate == null) {
            setError('must enter start and end date')
            return;
        }
        // if(maxPrice<0){
        //     setError('must enter posetive price')
        //     return;  
        // }
        try{
            const response= await axios.get('https://localhost:7125/Dijkstra/shortest-path',{
                params:{
                    CityCode: selectedDestinationCountry.code,
                    Origin: selectedOriginCountry.code,
                    Destination: selectedDestinationCountry.code,
                    DateStart: dayjs(startDate).format('YYYY-MM-DD'),
                    DateEnd: dayjs(endDate).format('YYYY-MM-DD'),
                    adult: numAdult, 
                    Radius: 30, // You might want to add this as a state variable
                    MaxPrice:maxPrice
                }
            });
            
        setPathData(response.data);
        setCurrentPage('results');
        }catch(error){
            console.error('Error fetching shortest path:', error);
            setError('Failed to fetch the shortest path. Please try again.');
        }
        





        event.preventDefault();
        const data = new FormData(event.currentTarget);
        // console.log({
        //     email: data.get('email'),
        //     password: data.get('password'),
        // });
        //NAVIGATE (TO PLACEOPTION)

        const getOptions = async () => {
            try {
                const response = await axios.get('https://localhost:7125/api/Travel/search-flights', {
                    params: {
                        origin: selectedOriginCountry,
                        destination: selectedDestinationCountry,
                        dateStart: startDate,
                        dateEnd: endDate,
                        price: 2500
                    }
                })
                console.log('00000', response.data);

            } catch (error) {
                console.log(error);
            }

        }
        getOptions();

    };

    return (
        <ThemeProvider theme={defaultTheme} >
            <Container component="main" maxWidth="xs" sx={{ right: '20vw', position: 'relative', top: '25vh' }}>
                <CssBaseline />
                <Box
                    sx={{
                        marginTop: 8,
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        position: 'relative',
                        left: '0vw'
                    }}
                >
                    <Typography component="h1" variant="h5">
                        My Form
                    </Typography>
                    <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 1 }}>

                        <Autocomplete
                            fullWidth
                            value={selectedOriginCountry.city}
                            onChange={(event, value) => selectedCountry("origin", value)}
                            options={countriesOriginOptions}
                            renderInput={(params) => <TextField {...params} label="Origin Country" />}
                        />

                        <Autocomplete
                            fullWidth
                            value={selectedDestinationCountry.city}
                            onChange={(event, value) => selectedCountry("destination", value)}
                            options={countriesDestinationOptions}
                            filterOptions={(options, state) => {
                                return options.filter((option) =>
                                    option.toLowerCase().includes(state.inputValue.toLowerCase())
                                );
                            }}
                            renderInput={(params) => <TextField {...params} label="Destination Country" />}
                        />
                        <FormControlLabel control={<Checkbox onClick={(e) => {
                            handleCheckBox(e)
                            console.log(e.target.checked);
                        }} />} label="give me population destination countries" />

                        {/* <DatePicker
                            selected={startDate}
                            onChange={(date) => setStartDate(date)}
                            selectsStart
                            startDate={startDate}
                            endDate={endDate}
                            minDate={new Date()}
                            placeholderText="MM/DD/YYYY"
                        />
                        <DatePicker
                            selected={endDate}
                            onChange={(date) => setEndDate(date)}
                            selectsEnd
                            startDate={startDate}
                            endDate={endDate}
                            minDate={startDate}
                            placeholderText="MM/DD/YYYY"
                        /> */}

                        <TextField
                            margin="normal"
                            required
                            fullWidth
                            id="maxPrice"
                            type="number"
                            label="Price"
                            name="maxPrice"
                            value={maxPrice}
                            onChange={(e)=>setMaxPrice(e.target.value)}
                            autoComplete="price"
                            autoFocus
                        />
                        

                        <TextField
                            margin="normal"
                            required
                            fullWidth
                            id="numAdult"
                            type="number"
                            label="num of travels"
                            name="numAdult"
                            value={numAdult}
                            onChange={(e)=>setnumAdult(e.target.value)}
                            autoComplete="num of travels"
                            autoFocus
                        />


                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DatePicker
                                value={startDate}
                                label="Start Date"
                                minDate={getTodayDate()}
                                onChange={(date) => { setStartDate(date) }}
                                selected={startDate}
                                selectsStart
                                startDate={startDate}
                                endDate={endDate}
                            />
                        </LocalizationProvider>
                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DatePicker
                                label="End Date"
                                minDate={startDate}
                                onChange={(date) => { setEndDate(date) }}
                            />
                        </LocalizationProvider>
                        {error && <h5 style={{ color: 'red' }}>{error}</h5>}

                        <Button
                            type="submit"
                            fullWidth
                            variant="contained"
                            sx={{ mt: 3, mb: 2 }}
                        >
                            Send
                        </Button>

                    </Box>
                </Box>
            </Container>
            {/* <ResultsPage></ResultsPage> */}
        </ThemeProvider>
    );
}





// import React, { useState } from 'react';
// import Button from '@mui/material/Button';
// import CssBaseline from '@mui/material/CssBaseline';
// import TextField from '@mui/material/TextField';
// import Box from '@mui/material/Box';
// import Typography from '@mui/material/Typography';
// import Container from '@mui/material/Container';
// import { createTheme, ThemeProvider } from '@mui/material/styles';
// import Autocomplete from '@mui/material/Autocomplete';
// import axios from 'axios';

// // Import the JSON data
// import iataCodes from '../IATA_CODE.json';

// const defaultTheme = createTheme();

// export default function HomePage() {
//     const [selectedOriginCountry, setSelectedOriginCountry] = useState('');
//     const [selectedDestinationCountry, setSelectedDestinationCountry] = useState('');
//     const [startDate, setStartDate] = useState(null);
//     const [endDate, setEndDate] = useState(null);

//     const currentDate = new Date();

//     const handleSubmit = async (event) => {
//         event.preventDefault();
//         try {
//             const response = await axios.get('https://localhost:7125/api/Travel/search-flights', {
//                 params: {
//                     origin: selectedOriginCountry,
//                     destination: selectedDestinationCountry,
//                     date: startDate,
//                     price: 2500
//                 }
//             });
//             console.log('Response:', response.data);
//         } catch (error) {
//             console.error('Error:', error);
//         }
//     };

//     return (
//         <ThemeProvider theme={defaultTheme}>
//             <Container component="main" maxWidth="xs">
//                 <CssBaseline />
//                 <Box
//                     sx={{
//                         marginTop: 8,
//                         display: 'flex',
//                         flexDirection: 'column',
//                         alignItems: 'center',
//                     }}
//                 >
//                     <Typography component="h1" variant="h5">
//                         My Form
//                     </Typography>
//                     <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 1 }}>
//                         <Autocomplete
//                             fullWidth
//                             value={selectedOriginCountry}
//                             onChange={(event, value) => setSelectedOriginCountry(value)}
//                             options={iataCodes.map((option) => `${option.code} - ${option.city}`)}
//                             renderInput={(params) => <TextField {...params} label="Origin Country" />}
//                         />
//                         <Autocomplete
//                             fullWidth
//                             value={selectedDestinationCountry}
//                             onChange={(event, value) => setSelectedDestinationCountry(value)}
//                             options={iataCodes.map((option) => `${option.code} - ${option.city}`)}
//                             renderInput={(params) => <TextField {...params} label="Destination Country" />}
//                         />

//                         <TextField
//                             margin="normal"
//                             required
//                             fullWidth
//                             id="price"
//                             type="number"
//                             label="Price"
//                             name="price"
//                             autoComplete="price"
//                             autoFocus
//                         />

//                         <Button
//                             type="submit"
//                             fullWidth
//                             variant="contained"
//                             sx={{ mt: 3, mb: 2 }}
//                         >
//                             Send
//                         </Button>

//                     </Box>
//                 </Box>
//             </Container>
//         </ThemeProvider>
//     );
// }
